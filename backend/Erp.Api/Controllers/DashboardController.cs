using Erp.Api.Data;
using Erp.Api.Dtos;
using Erp.Api.Models;
using Erp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardController(ErpDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var materials = await db.Materials
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Where(x => x.IsActive)
            .ToListAsync();

        var materialIds = materials.Select(x => x.Id).ToHashSet();
        var movements = await db.StockMovements
            .AsNoTracking()
            .Where(x => materialIds.Contains(x.MaterialId))
            .Select(x => new { x.MaterialId, x.Type, x.Quantity })
            .ToListAsync();

        var stockByMaterial = movements
            .GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.Sum(m => InventoryMath.SignedQuantity(m.Type, m.Quantity)));

        var balances = materials
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

        var openStatuses = new[] { OrderStatus.Draft, OrderStatus.Confirmed, OrderStatus.PartiallyReceived, OrderStatus.PartiallyShipped };
        var inventoryValue = materials.Sum(x => stockByMaterial.GetValueOrDefault(x.Id) * x.StandardCost);
        var today = DateTime.UtcNow.Date;
        var openPayables = await db.FinancialEntries
            .AsNoTracking()
            .Where(x => x.Type == FinancialEntryType.Payable && x.Status == FinancialEntryStatus.Open)
            .SumAsync(x => x.Amount - x.PaidAmount);
        var openReceivables = await db.FinancialEntries
            .AsNoTracking()
            .Where(x => x.Type == FinancialEntryType.Receivable && x.Status == FinancialEntryStatus.Open)
            .SumAsync(x => x.Amount - x.PaidAmount);
        var overdueFinancialEntries = await db.FinancialEntries
            .AsNoTracking()
            .CountAsync(x => x.Status == FinancialEntryStatus.Open && x.DueDate.Date < today);

        var dashboard = new DashboardDto(
            ActiveMaterials: materials.Count,
            ActiveCustomers: await db.Customers.CountAsync(x => x.IsActive),
            ActiveSuppliers: await db.Suppliers.CountAsync(x => x.IsActive),
            OpenPurchaseOrders: await db.PurchaseOrders.CountAsync(x => openStatuses.Contains(x.Status)),
            OpenSalesOrders: await db.SalesOrders.CountAsync(x => openStatuses.Contains(x.Status)),
            LowStockMaterials: balances.Count(x => x.BelowMinimum),
            InventoryValue: inventoryValue,
            OpenPayables: openPayables,
            OpenReceivables: openReceivables,
            OverdueFinancialEntries: overdueFinancialEntries,
            LowStockItems: balances.Where(x => x.BelowMinimum).OrderBy(x => x.MaterialCode).Take(10).ToList());

        return Ok(dashboard);
    }
}
