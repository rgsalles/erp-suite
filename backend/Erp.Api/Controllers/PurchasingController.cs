using Erp.Api.Data;
using Erp.Api.Dtos;
using Erp.Api.Messaging;
using Erp.Api.Models;
using Erp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PurchasingController(
    ErpDbContext db,
    IIntegrationEventPublisher eventPublisher,
    FinancialEntryService financialEntryService) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyCollection<PurchaseOrderDto>>> GetOrders()
    {
        var orders = await IncludeOrderGraph(db.PurchaseOrders.AsNoTracking())
            .OrderByDescending(x => x.OrderDate)
            .Take(100)
            .ToListAsync();

        return Ok(orders.Select(ToDto).ToList());
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetOrder(Guid id)
    {
        var order = await IncludeOrderGraph(db.PurchaseOrders.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == id);

        return order is null ? NotFound() : Ok(ToDto(order));
    }

    [HttpPost("orders")]
    [Authorize(Roles = "Admin,Manager,Buyer")]
    public async Task<ActionResult<PurchaseOrderDto>> CreateOrder(CreatePurchaseOrderRequest request)
    {
        if (!await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId && x.IsActive))
        {
            return BadRequest("Invalid supplier.");
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("At least one item is required.");
        }

        var materialIds = request.Items.Select(x => x.MaterialId).ToHashSet();
        var existingMaterials = await db.Materials
            .Where(x => materialIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();

        if (existingMaterials.Count != materialIds.Count)
        {
            return BadRequest("One or more materials are invalid.");
        }

        var order = new PurchaseOrder
        {
            Number = await GenerateNumberAsync("PO"),
            SupplierId = request.SupplierId,
            ExpectedDate = request.ExpectedDate,
            Notes = request.Notes,
            Status = OrderStatus.Confirmed,
            Items = request.Items.Select(x => new PurchaseOrderItem
            {
                MaterialId = x.MaterialId,
                Quantity = x.Quantity,
                UnitCost = x.UnitCost
            }).ToList()
        };

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, await GetOrderDtoAsync(order.Id));
    }

    [HttpPost("orders/{id:guid}/receive")]
    [Authorize(Roles = "Admin,Manager,Buyer,Stock")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(Guid id, ReceivePurchaseOrderRequest request)
    {
        var warehouseExists = await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId && x.IsActive);
        if (!warehouseExists)
        {
            return BadRequest("Invalid warehouse.");
        }

        var order = await db.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Received)
        {
            return BadRequest("This purchase order cannot be received.");
        }

        var createdMovements = new List<StockMovement>();

        foreach (var item in order.Items)
        {
            var remaining = item.Quantity - item.ReceivedQuantity;
            if (remaining <= 0)
            {
                continue;
            }

            item.ReceivedQuantity = item.Quantity;
            var movement = new StockMovement
            {
                MaterialId = item.MaterialId,
                WarehouseId = request.WarehouseId,
                Type = StockMovementType.PurchaseReceipt,
                Quantity = remaining,
                UnitCost = item.UnitCost,
                Reference = order.Number,
                Notes = "Purchase receipt",
                CreatedByUserId = User.GetUserId(),
                MovementDate = DateTime.UtcNow
            };

            createdMovements.Add(movement);
            db.StockMovements.Add(movement);
        }

        order.Status = OrderStatus.Received;
        order.ReceivedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await financialEntryService.CreatePayableForPurchaseOrderAsync(order.Id, User.GetUserId(), HttpContext.RequestAborted);

        foreach (var movement in createdMovements)
        {
            await PublishStockMovementCreatedAsync(movement.Id);
        }

        await eventPublisher.PublishAsync(
            RabbitMqRoutingKeys.PurchaseOrderReceived,
            new PurchaseOrderReceivedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTime.UtcNow,
                PurchaseOrderId: order.Id,
                Number: order.Number,
                SupplierId: order.SupplierId,
                WarehouseId: request.WarehouseId,
                UserId: User.GetUserId()),
            HttpContext.RequestAborted);

        return Ok(await GetOrderDtoAsync(order.Id));
    }

    [HttpPost("orders/{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Manager,Buyer")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(Guid id)
    {
        var order = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status == OrderStatus.Received)
        {
            return BadRequest("Received orders cannot be cancelled.");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(await GetOrderDtoAsync(order.Id));
    }

    private async Task<PurchaseOrderDto> GetOrderDtoAsync(Guid id)
    {
        var order = await IncludeOrderGraph(db.PurchaseOrders.AsNoTracking()).FirstAsync(x => x.Id == id);
        return ToDto(order);
    }

    private async Task<string> GenerateNumberAsync(string prefix)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var count = await db.PurchaseOrders.CountAsync(x => x.OrderDate >= today && x.OrderDate < tomorrow);
        return $"{prefix}-{today:yyyyMMdd}-{count + 1:0000}";
    }

    private static IQueryable<PurchaseOrder> IncludeOrderGraph(IQueryable<PurchaseOrder> query)
    {
        return query
            .Include(x => x.Supplier)
            .Include(x => x.Items)
            .ThenInclude(x => x.Material);
    }

    private static PurchaseOrderDto ToDto(PurchaseOrder order)
    {
        var items = order.Items.Select(item => new PurchaseOrderItemDto(
            item.Id,
            item.MaterialId,
            item.Material?.Code ?? string.Empty,
            item.Material?.Description ?? string.Empty,
            item.Quantity,
            item.UnitCost,
            item.ReceivedQuantity,
            item.Quantity * item.UnitCost)).ToList();

        return new PurchaseOrderDto(
            order.Id,
            order.Number,
            order.SupplierId,
            order.Supplier?.Name ?? string.Empty,
            order.Status,
            order.OrderDate,
            order.ExpectedDate,
            order.ReceivedAt,
            order.Notes,
            items.Sum(x => x.LineTotal),
            items);
    }

    private async Task PublishStockMovementCreatedAsync(Guid stockMovementId)
    {
        var movement = await db.StockMovements
            .AsNoTracking()
            .Include(x => x.Material)
            .Include(x => x.Warehouse)
            .FirstAsync(x => x.Id == stockMovementId);

        var currentStock = await GetCurrentStockAsync(movement.MaterialId);

        await eventPublisher.PublishAsync(
            RabbitMqRoutingKeys.StockMovementCreated,
            new StockMovementCreatedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTime.UtcNow,
                StockMovementId: movement.Id,
                MaterialId: movement.MaterialId,
                MaterialCode: movement.Material?.Code ?? string.Empty,
                MaterialDescription: movement.Material?.Description ?? string.Empty,
                WarehouseId: movement.WarehouseId,
                WarehouseCode: movement.Warehouse?.Code ?? string.Empty,
                MovementType: movement.Type,
                Quantity: movement.Quantity,
                SignedQuantity: InventoryMath.SignedQuantity(movement.Type, movement.Quantity),
                CurrentStock: currentStock,
                MinimumStock: movement.Material?.MinimumStock ?? 0,
                UserId: User.GetUserId(),
                Reference: movement.Reference),
            HttpContext.RequestAborted);
    }

    private async Task<decimal> GetCurrentStockAsync(Guid materialId)
    {
        var movements = await db.StockMovements
            .AsNoTracking()
            .Where(x => x.MaterialId == materialId)
            .Select(x => new { x.Type, x.Quantity })
            .ToListAsync();

        return movements.Sum(x => InventoryMath.SignedQuantity(x.Type, x.Quantity));
    }
}
