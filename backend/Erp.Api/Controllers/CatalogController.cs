using Erp.Api.Data;
using Erp.Api.Dtos;
using Erp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CatalogController(ErpDbContext db) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyCollection<CatalogItemDto>>> GetCategories()
    {
        return Ok(await db.MaterialCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CatalogItemDto(x.Id, x.Name, x.Description))
            .ToListAsync());
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<CatalogItemDto>> CreateCategory(SaveCatalogItemRequest request)
    {
        var category = new MaterialCategory { Name = request.Name.Trim(), Description = request.Description };
        db.MaterialCategories.Add(category);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCategories), new CatalogItemDto(category.Id, category.Name, category.Description));
    }

    [HttpPut("categories/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<CatalogItemDto>> UpdateCategory(Guid id, SaveCatalogItemRequest request)
    {
        var category = await db.MaterialCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new CatalogItemDto(category.Id, category.Name, category.Description));
    }

    [HttpGet("units")]
    public async Task<ActionResult<IReadOnlyCollection<UnitOfMeasureDto>>> GetUnits()
    {
        return Ok(await db.UnitOfMeasures
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new UnitOfMeasureDto(x.Id, x.Code, x.Name))
            .ToListAsync());
    }

    [HttpPost("units")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<UnitOfMeasureDto>> CreateUnit(SaveUnitOfMeasureRequest request)
    {
        var unit = new UnitOfMeasure { Code = request.Code.Trim().ToUpperInvariant(), Name = request.Name.Trim() };
        db.UnitOfMeasures.Add(unit);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUnits), new UnitOfMeasureDto(unit.Id, unit.Code, unit.Name));
    }

    [HttpPut("units/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<UnitOfMeasureDto>> UpdateUnit(Guid id, SaveUnitOfMeasureRequest request)
    {
        var unit = await db.UnitOfMeasures.FindAsync(id);
        if (unit is null)
        {
            return NotFound();
        }

        unit.Code = request.Code.Trim().ToUpperInvariant();
        unit.Name = request.Name.Trim();
        unit.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new UnitOfMeasureDto(unit.Id, unit.Code, unit.Name));
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<IReadOnlyCollection<BusinessPartnerDto>>> GetSuppliers()
    {
        return Ok(await db.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new BusinessPartnerDto(x.Id, x.Name, x.TaxId, x.Email, x.Phone, x.ContactName, x.IsActive))
            .ToListAsync());
    }

    [HttpPost("suppliers")]
    [Authorize(Roles = "Admin,Manager,Buyer")]
    public async Task<ActionResult<BusinessPartnerDto>> CreateSupplier(SaveBusinessPartnerRequest request)
    {
        var supplier = new Supplier();
        ApplyPartner(supplier, request);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSuppliers), ToDto(supplier));
    }

    [HttpPut("suppliers/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Buyer")]
    public async Task<ActionResult<BusinessPartnerDto>> UpdateSupplier(Guid id, SaveBusinessPartnerRequest request)
    {
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        ApplyPartner(supplier, request);
        supplier.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(supplier));
    }

    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyCollection<BusinessPartnerDto>>> GetCustomers()
    {
        return Ok(await db.Customers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new BusinessPartnerDto(x.Id, x.Name, x.TaxId, x.Email, x.Phone, x.ContactName, x.IsActive))
            .ToListAsync());
    }

    [HttpPost("customers")]
    [Authorize(Roles = "Admin,Manager,Seller")]
    public async Task<ActionResult<BusinessPartnerDto>> CreateCustomer(SaveBusinessPartnerRequest request)
    {
        var customer = new Customer();
        ApplyPartner(customer, request);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCustomers), ToDto(customer));
    }

    [HttpPut("customers/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Seller")]
    public async Task<ActionResult<BusinessPartnerDto>> UpdateCustomer(Guid id, SaveBusinessPartnerRequest request)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        ApplyPartner(customer, request);
        customer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(customer));
    }

    [HttpGet("warehouses")]
    public async Task<ActionResult<IReadOnlyCollection<WarehouseDto>>> GetWarehouses()
    {
        return Ok(await db.Warehouses
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.Location, x.IsActive))
            .ToListAsync());
    }

    [HttpPost("warehouses")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse(SaveWarehouseRequest request)
    {
        var warehouse = new Warehouse();
        ApplyWarehouse(warehouse, request);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetWarehouses), ToDto(warehouse));
    }

    [HttpPut("warehouses/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(Guid id, SaveWarehouseRequest request)
    {
        var warehouse = await db.Warehouses.FindAsync(id);
        if (warehouse is null)
        {
            return NotFound();
        }

        ApplyWarehouse(warehouse, request);
        warehouse.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(warehouse));
    }

    private static void ApplyPartner(Supplier partner, SaveBusinessPartnerRequest request)
    {
        partner.Name = request.Name.Trim();
        partner.TaxId = request.TaxId.Trim();
        partner.Email = request.Email;
        partner.Phone = request.Phone;
        partner.ContactName = request.ContactName;
        partner.IsActive = request.IsActive;
    }

    private static void ApplyPartner(Customer partner, SaveBusinessPartnerRequest request)
    {
        partner.Name = request.Name.Trim();
        partner.TaxId = request.TaxId.Trim();
        partner.Email = request.Email;
        partner.Phone = request.Phone;
        partner.ContactName = request.ContactName;
        partner.IsActive = request.IsActive;
    }

    private static BusinessPartnerDto ToDto(Supplier supplier)
    {
        return new BusinessPartnerDto(supplier.Id, supplier.Name, supplier.TaxId, supplier.Email, supplier.Phone, supplier.ContactName, supplier.IsActive);
    }

    private static BusinessPartnerDto ToDto(Customer customer)
    {
        return new BusinessPartnerDto(customer.Id, customer.Name, customer.TaxId, customer.Email, customer.Phone, customer.ContactName, customer.IsActive);
    }

    private static void ApplyWarehouse(Warehouse warehouse, SaveWarehouseRequest request)
    {
        warehouse.Code = request.Code.Trim().ToUpperInvariant();
        warehouse.Name = request.Name.Trim();
        warehouse.Location = request.Location;
        warehouse.IsActive = request.IsActive;
    }

    private static WarehouseDto ToDto(Warehouse warehouse)
    {
        return new WarehouseDto(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Location, warehouse.IsActive);
    }
}
