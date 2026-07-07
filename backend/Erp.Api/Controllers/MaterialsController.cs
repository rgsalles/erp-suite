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
public sealed class MaterialsController(ErpDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MaterialDto>>> Get([FromQuery] string? search, [FromQuery] bool includeInactive = false)
    {
        var query = db.Materials
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Supplier)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Description.Contains(term));
        }

        var materials = await query.OrderBy(x => x.Code).ToListAsync();
        var stockByMaterial = await GetStockByMaterialAsync(materials.Select(x => x.Id));

        return Ok(materials.Select(x => ToDto(x, stockByMaterial.GetValueOrDefault(x.Id))).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaterialDto>> GetById(Guid id)
    {
        var material = await db.Materials
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (material is null)
        {
            return NotFound();
        }

        var stockByMaterial = await GetStockByMaterialAsync([material.Id]);
        return Ok(ToDto(material, stockByMaterial.GetValueOrDefault(material.Id)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<MaterialDto>> Create(SaveMaterialRequest request)
    {
        if (await db.Materials.AnyAsync(x => x.Code == request.Code.Trim().ToUpperInvariant()))
        {
            return Conflict("A material with this code already exists.");
        }

        if (!await HasRequiredReferencesAsync(request))
        {
            return BadRequest("Invalid category, unit of measure or supplier.");
        }

        var material = new Material();
        Apply(material, request);

        db.Materials.Add(material);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = material.Id }, await GetMaterialDtoAsync(material.Id));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<MaterialDto>> Update(Guid id, SaveMaterialRequest request)
    {
        var material = await db.Materials.FirstOrDefaultAsync(x => x.Id == id);
        if (material is null)
        {
            return NotFound();
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Materials.AnyAsync(x => x.Id != id && x.Code == code))
        {
            return Conflict("A material with this code already exists.");
        }

        if (!await HasRequiredReferencesAsync(request))
        {
            return BadRequest("Invalid category, unit of measure or supplier.");
        }

        Apply(material, request);
        material.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(await GetMaterialDtoAsync(material.Id));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var material = await db.Materials.FirstOrDefaultAsync(x => x.Id == id);
        if (material is null)
        {
            return NotFound();
        }

        material.IsActive = false;
        material.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> HasRequiredReferencesAsync(SaveMaterialRequest request)
    {
        var categoryExists = await db.MaterialCategories.AnyAsync(x => x.Id == request.CategoryId);
        var unitExists = await db.UnitOfMeasures.AnyAsync(x => x.Id == request.UnitOfMeasureId);
        var supplierExists = request.SupplierId is null || await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId && x.IsActive);

        return categoryExists && unitExists && supplierExists;
    }

    private async Task<MaterialDto> GetMaterialDtoAsync(Guid id)
    {
        var material = await db.Materials
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Supplier)
            .FirstAsync(x => x.Id == id);

        var stockByMaterial = await GetStockByMaterialAsync([id]);
        return ToDto(material, stockByMaterial.GetValueOrDefault(id));
    }

    private async Task<Dictionary<Guid, decimal>> GetStockByMaterialAsync(IEnumerable<Guid> materialIds)
    {
        var ids = materialIds.ToHashSet();
        var movements = await db.StockMovements
            .AsNoTracking()
            .Where(x => ids.Contains(x.MaterialId))
            .Select(x => new { x.MaterialId, x.Type, x.Quantity })
            .ToListAsync();

        return movements
            .GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.Sum(m => InventoryMath.SignedQuantity(m.Type, m.Quantity)));
    }

    private static void Apply(Material material, SaveMaterialRequest request)
    {
        material.Code = request.Code.Trim().ToUpperInvariant();
        material.Description = request.Description.Trim();
        material.CategoryId = request.CategoryId;
        material.UnitOfMeasureId = request.UnitOfMeasureId;
        material.SupplierId = request.SupplierId;
        material.StandardCost = request.StandardCost;
        material.SalePrice = request.SalePrice;
        material.MinimumStock = request.MinimumStock;
        material.IsActive = request.IsActive;
    }

    private static MaterialDto ToDto(Material material, decimal stock)
    {
        return new MaterialDto(
            material.Id,
            material.Code,
            material.Description,
            material.CategoryId,
            material.Category?.Name ?? string.Empty,
            material.UnitOfMeasureId,
            material.UnitOfMeasure?.Code ?? string.Empty,
            material.SupplierId,
            material.Supplier?.Name,
            material.StandardCost,
            material.SalePrice,
            material.MinimumStock,
            stock,
            material.IsActive);
    }
}
