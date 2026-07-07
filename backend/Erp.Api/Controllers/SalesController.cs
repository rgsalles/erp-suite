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
public sealed class SalesController(
    ErpDbContext db,
    IIntegrationEventPublisher eventPublisher,
    FinancialEntryService financialEntryService) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyCollection<SalesOrderDto>>> GetOrders()
    {
        var orders = await IncludeOrderGraph(db.SalesOrders.AsNoTracking())
            .OrderByDescending(x => x.OrderDate)
            .Take(100)
            .ToListAsync();

        return Ok(orders.Select(ToDto).ToList());
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> GetOrder(Guid id)
    {
        var order = await IncludeOrderGraph(db.SalesOrders.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == id);

        return order is null ? NotFound() : Ok(ToDto(order));
    }

    [HttpPost("orders")]
    [Authorize(Roles = "Admin,Manager,Seller")]
    public async Task<ActionResult<SalesOrderDto>> CreateOrder(CreateSalesOrderRequest request)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive))
        {
            return BadRequest("Invalid customer.");
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

        var order = new SalesOrder
        {
            Number = await GenerateNumberAsync("SO"),
            CustomerId = request.CustomerId,
            Notes = request.Notes,
            Status = OrderStatus.Confirmed,
            Items = request.Items.Select(x => new SalesOrderItem
            {
                MaterialId = x.MaterialId,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice
            }).ToList()
        };

        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, await GetOrderDtoAsync(order.Id));
    }

    [HttpPost("orders/{id:guid}/ship")]
    [Authorize(Roles = "Admin,Manager,Seller,Stock")]
    public async Task<ActionResult<SalesOrderDto>> Ship(Guid id, ShipSalesOrderRequest request)
    {
        var warehouseExists = await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId && x.IsActive);
        if (!warehouseExists)
        {
            return BadRequest("Invalid warehouse.");
        }

        var order = await db.SalesOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Shipped)
        {
            return BadRequest("This sales order cannot be shipped.");
        }

        var materialIds = order.Items.Select(x => x.MaterialId).ToHashSet();
        var movements = await db.StockMovements
            .AsNoTracking()
            .Where(x => materialIds.Contains(x.MaterialId) && x.WarehouseId == request.WarehouseId)
            .Select(x => new { x.MaterialId, x.Type, x.Quantity })
            .ToListAsync();

        var stockByMaterial = movements
            .GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.Sum(m => InventoryMath.SignedQuantity(m.Type, m.Quantity)));

        foreach (var item in order.Items)
        {
            var remaining = item.Quantity - item.ShippedQuantity;
            var available = stockByMaterial.GetValueOrDefault(item.MaterialId);

            if (remaining > available)
            {
                return BadRequest($"Not enough stock for material {item.MaterialId}.");
            }
        }

        var createdMovements = new List<StockMovement>();

        foreach (var item in order.Items)
        {
            var remaining = item.Quantity - item.ShippedQuantity;
            if (remaining <= 0)
            {
                continue;
            }

            item.ShippedQuantity = item.Quantity;
            var movement = new StockMovement
            {
                MaterialId = item.MaterialId,
                WarehouseId = request.WarehouseId,
                Type = StockMovementType.SalesShipment,
                Quantity = remaining,
                Reference = order.Number,
                Notes = "Sales shipment",
                CreatedByUserId = User.GetUserId(),
                MovementDate = DateTime.UtcNow
            };

            createdMovements.Add(movement);
            db.StockMovements.Add(movement);
        }

        order.Status = OrderStatus.Shipped;
        order.ShippedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await financialEntryService.CreateReceivableForSalesOrderAsync(order.Id, User.GetUserId(), HttpContext.RequestAborted);

        foreach (var movement in createdMovements)
        {
            await PublishStockMovementCreatedAsync(movement.Id);
        }

        await eventPublisher.PublishAsync(
            RabbitMqRoutingKeys.SalesOrderShipped,
            new SalesOrderShippedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTime.UtcNow,
                SalesOrderId: order.Id,
                Number: order.Number,
                CustomerId: order.CustomerId,
                WarehouseId: request.WarehouseId,
                UserId: User.GetUserId()),
            HttpContext.RequestAborted);

        return Ok(await GetOrderDtoAsync(order.Id));
    }

    [HttpPost("orders/{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Manager,Seller")]
    public async Task<ActionResult<SalesOrderDto>> Cancel(Guid id)
    {
        var order = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status == OrderStatus.Shipped)
        {
            return BadRequest("Shipped orders cannot be cancelled.");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(await GetOrderDtoAsync(order.Id));
    }

    private async Task<SalesOrderDto> GetOrderDtoAsync(Guid id)
    {
        var order = await IncludeOrderGraph(db.SalesOrders.AsNoTracking()).FirstAsync(x => x.Id == id);
        return ToDto(order);
    }

    private async Task<string> GenerateNumberAsync(string prefix)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var count = await db.SalesOrders.CountAsync(x => x.OrderDate >= today && x.OrderDate < tomorrow);
        return $"{prefix}-{today:yyyyMMdd}-{count + 1:0000}";
    }

    private static IQueryable<SalesOrder> IncludeOrderGraph(IQueryable<SalesOrder> query)
    {
        return query
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .ThenInclude(x => x.Material);
    }

    private static SalesOrderDto ToDto(SalesOrder order)
    {
        var items = order.Items.Select(item => new SalesOrderItemDto(
            item.Id,
            item.MaterialId,
            item.Material?.Code ?? string.Empty,
            item.Material?.Description ?? string.Empty,
            item.Quantity,
            item.UnitPrice,
            item.ShippedQuantity,
            item.Quantity * item.UnitPrice)).ToList();

        return new SalesOrderDto(
            order.Id,
            order.Number,
            order.CustomerId,
            order.Customer?.Name ?? string.Empty,
            order.Status,
            order.OrderDate,
            order.ShippedAt,
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
