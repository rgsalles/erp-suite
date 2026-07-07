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
public sealed class InventoryController(ErpDbContext db, IIntegrationEventPublisher eventPublisher) : ControllerBase
{
    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyCollection<StockBalanceDto>>> GetBalances()
    {
        var materials = await db.Materials
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync();

        var balances = await BuildBalancesAsync(materials);
        return Ok(balances);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<IReadOnlyCollection<StockMovementDto>>> GetMovements([FromQuery] Guid? materialId)
    {
        var query = db.StockMovements
            .AsNoTracking()
            .Include(x => x.Material)
            .Include(x => x.Warehouse)
            .AsQueryable();

        if (materialId.HasValue)
        {
            query = query.Where(x => x.MaterialId == materialId);
        }

        var movements = await query
            .OrderByDescending(x => x.MovementDate)
            .Take(250)
            .ToListAsync();

        return Ok(movements.Select(ToDto).ToList());
    }

    [HttpPost("movements")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<StockMovementDto>> CreateMovement(CreateStockMovementRequest request)
    {
        var materialExists = await db.Materials.AnyAsync(x => x.Id == request.MaterialId && x.IsActive);
        var warehouseExists = await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId && x.IsActive);

        if (!materialExists || !warehouseExists)
        {
            return BadRequest("Invalid material or warehouse.");
        }

        var movement = new StockMovement
        {
            MaterialId = request.MaterialId,
            WarehouseId = request.WarehouseId,
            Type = request.Type,
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            Reference = request.Reference,
            Notes = request.Notes,
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = User.GetUserId()
        };

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync();

        var saved = await db.StockMovements
            .AsNoTracking()
            .Include(x => x.Material)
            .Include(x => x.Warehouse)
            .FirstAsync(x => x.Id == movement.Id);

        await PublishStockMovementCreatedAsync(saved, User.GetUserId());

        return CreatedAtAction(nameof(GetMovements), new { materialId = movement.MaterialId }, ToDto(saved));
    }

    private async Task<IReadOnlyCollection<StockBalanceDto>> BuildBalancesAsync(IReadOnlyCollection<Material> materials)
    {
        var materialIds = materials.Select(x => x.Id).ToHashSet();
        var movements = await db.StockMovements
            .AsNoTracking()
            .Where(x => materialIds.Contains(x.MaterialId))
            .Select(x => new { x.MaterialId, x.Type, x.Quantity })
            .ToListAsync();

        var stockByMaterial = movements
            .GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.Sum(m => InventoryMath.SignedQuantity(m.Type, m.Quantity)));

        return materials
            .Select(material =>
            {
                var stock = stockByMaterial.GetValueOrDefault(material.Id);
                return new StockBalanceDto(
                    material.Id,
                    material.Code,
                    material.Description,
                    material.UnitOfMeasure?.Code ?? string.Empty,
                    material.MinimumStock,
                    stock,
                    stock < material.MinimumStock);
            })
            .ToList();
    }

    private static StockMovementDto ToDto(StockMovement movement)
    {
        return new StockMovementDto(
            movement.Id,
            movement.MaterialId,
            movement.Material?.Code ?? string.Empty,
            movement.Material?.Description ?? string.Empty,
            movement.WarehouseId,
            movement.Warehouse?.Code ?? string.Empty,
            movement.Type,
            movement.Quantity,
            InventoryMath.SignedQuantity(movement.Type, movement.Quantity),
            movement.UnitCost,
            movement.Reference,
            movement.Notes,
            movement.MovementDate);
    }

    private async Task PublishStockMovementCreatedAsync(StockMovement movement, Guid? userId)
    {
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
                UserId: userId,
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
