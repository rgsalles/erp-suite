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
    [HttpGet("companies")]
    public async Task<ActionResult<IReadOnlyCollection<CompanyDto>>> GetCompanies()
    {
        return Ok(await db.Companies
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new CompanyDto(x.Id, x.Code, x.Name, x.TaxId, x.IsActive))
            .ToListAsync());
    }

    [HttpPost("companies")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CompanyDto>> CreateCompany(SaveCompanyRequest request)
    {
        var company = new Company();
        ApplyCompany(company, request);
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCompanies), ToDto(company));
    }

    [HttpPut("companies/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CompanyDto>> UpdateCompany(Guid id, SaveCompanyRequest request)
    {
        var company = await db.Companies.FindAsync(id);
        if (company is null)
        {
            return NotFound();
        }

        ApplyCompany(company, request);
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(company));
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IReadOnlyCollection<BranchDto>>> GetBranches()
    {
        var branches = await db.Branches
            .AsNoTracking()
            .Include(x => x.Company)
            .OrderBy(x => x.Company!.Code)
            .ThenBy(x => x.Code)
            .ToListAsync();

        return Ok(branches.Select(ToDto).ToList());
    }

    [HttpPost("branches")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<BranchDto>> CreateBranch(SaveBranchRequest request)
    {
        var validationError = await GetCompanyValidationErrorAsync(request.CompanyId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var branch = new Branch();
        ApplyBranch(branch, request);
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var saved = await db.Branches.AsNoTracking().Include(x => x.Company).FirstAsync(x => x.Id == branch.Id);
        return CreatedAtAction(nameof(GetBranches), ToDto(saved));
    }

    [HttpPut("branches/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<BranchDto>> UpdateBranch(Guid id, SaveBranchRequest request)
    {
        var branch = await db.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        var validationError = await GetCompanyValidationErrorAsync(request.CompanyId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        ApplyBranch(branch, request);
        branch.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Branches.AsNoTracking().Include(x => x.Company).FirstAsync(x => x.Id == branch.Id);
        return Ok(ToDto(saved));
    }

    [HttpGet("cost-centers")]
    public async Task<ActionResult<IReadOnlyCollection<CostCenterDto>>> GetCostCenters()
    {
        var costCenters = await db.CostCenters
            .AsNoTracking()
            .Include(x => x.Company)
            .OrderBy(x => x.Company!.Code)
            .ThenBy(x => x.Code)
            .ToListAsync();

        return Ok(costCenters.Select(ToDto).ToList());
    }

    [HttpPost("cost-centers")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CostCenterDto>> CreateCostCenter(SaveCostCenterRequest request)
    {
        var validationError = await GetCompanyValidationErrorAsync(request.CompanyId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var costCenter = new CostCenter();
        ApplyCostCenter(costCenter, request);
        db.CostCenters.Add(costCenter);
        await db.SaveChangesAsync();

        var saved = await db.CostCenters.AsNoTracking().Include(x => x.Company).FirstAsync(x => x.Id == costCenter.Id);
        return CreatedAtAction(nameof(GetCostCenters), ToDto(saved));
    }

    [HttpPut("cost-centers/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CostCenterDto>> UpdateCostCenter(Guid id, SaveCostCenterRequest request)
    {
        var costCenter = await db.CostCenters.FindAsync(id);
        if (costCenter is null)
        {
            return NotFound();
        }

        var validationError = await GetCompanyValidationErrorAsync(request.CompanyId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        ApplyCostCenter(costCenter, request);
        costCenter.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.CostCenters.AsNoTracking().Include(x => x.Company).FirstAsync(x => x.Id == costCenter.Id);
        return Ok(ToDto(saved));
    }

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

    [HttpGet("currencies")]
    public async Task<ActionResult<IReadOnlyCollection<CurrencyUnitDto>>> GetCurrencies()
    {
        return Ok(await db.CurrencyUnits
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .Select(x => new CurrencyUnitDto(x.Id, x.Code, x.Name, x.Symbol, x.IsDefault))
            .ToListAsync());
    }

    [HttpPost("currencies")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CurrencyUnitDto>> CreateCurrency(SaveCurrencyUnitRequest request)
    {
        var currency = new CurrencyUnit();
        await ApplyCurrencyAsync(currency, request);
        db.CurrencyUnits.Add(currency);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCurrencies), ToDto(currency));
    }

    [HttpPut("currencies/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CurrencyUnitDto>> UpdateCurrency(Guid id, SaveCurrencyUnitRequest request)
    {
        var currency = await db.CurrencyUnits.FindAsync(id);
        if (currency is null)
        {
            return NotFound();
        }

        await ApplyCurrencyAsync(currency, request);
        currency.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ToDto(currency));
    }

    [HttpGet("exchange-rates")]
    public async Task<ActionResult<IReadOnlyCollection<ExchangeRateDto>>> GetExchangeRates()
    {
        var exchangeRates = await db.ExchangeRates
            .AsNoTracking()
            .Include(x => x.FromCurrency)
            .Include(x => x.ToCurrency)
            .OrderByDescending(x => x.RateDate)
            .ThenBy(x => x.FromCurrency!.Code)
            .ThenBy(x => x.ToCurrency!.Code)
            .ToListAsync();

        return Ok(exchangeRates.Select(ToDto).ToList());
    }

    [HttpPost("exchange-rates")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ExchangeRateDto>> CreateExchangeRate(SaveExchangeRateRequest request)
    {
        var validationError = await GetExchangeRateValidationErrorAsync(request);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var exchangeRate = new ExchangeRate();
        ApplyExchangeRate(exchangeRate, request);
        db.ExchangeRates.Add(exchangeRate);
        await db.SaveChangesAsync();

        var saved = await db.ExchangeRates
            .AsNoTracking()
            .Include(x => x.FromCurrency)
            .Include(x => x.ToCurrency)
            .FirstAsync(x => x.Id == exchangeRate.Id);

        return CreatedAtAction(nameof(GetExchangeRates), ToDto(saved));
    }

    [HttpPut("exchange-rates/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ExchangeRateDto>> UpdateExchangeRate(Guid id, SaveExchangeRateRequest request)
    {
        var exchangeRate = await db.ExchangeRates.FindAsync(id);
        if (exchangeRate is null)
        {
            return NotFound();
        }

        var validationError = await GetExchangeRateValidationErrorAsync(request, id);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        ApplyExchangeRate(exchangeRate, request);
        exchangeRate.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.ExchangeRates
            .AsNoTracking()
            .Include(x => x.FromCurrency)
            .Include(x => x.ToCurrency)
            .FirstAsync(x => x.Id == exchangeRate.Id);

        return Ok(ToDto(saved));
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
        var warehouses = await db.Warehouses
            .AsNoTracking()
            .Include(x => x.Branch)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return Ok(warehouses.Select(ToDto).ToList());
    }

    [HttpPost("warehouses")]
    [Authorize(Roles = "Admin,Manager,Stock")]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse(SaveWarehouseRequest request)
    {
        var validationError = await GetBranchValidationErrorAsync(request.BranchId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var warehouse = new Warehouse();
        ApplyWarehouse(warehouse, request);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var saved = await db.Warehouses.AsNoTracking().Include(x => x.Branch).FirstAsync(x => x.Id == warehouse.Id);
        return CreatedAtAction(nameof(GetWarehouses), ToDto(saved));
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

        var validationError = await GetBranchValidationErrorAsync(request.BranchId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        ApplyWarehouse(warehouse, request);
        warehouse.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Warehouses.AsNoTracking().Include(x => x.Branch).FirstAsync(x => x.Id == warehouse.Id);
        return Ok(ToDto(saved));
    }

    private static void ApplyCompany(Company company, SaveCompanyRequest request)
    {
        company.Code = request.Code.Trim().ToUpperInvariant();
        company.Name = request.Name.Trim();
        company.TaxId = string.IsNullOrWhiteSpace(request.TaxId) ? null : request.TaxId.Trim();
        company.IsActive = request.IsActive;
    }

    private static CompanyDto ToDto(Company company)
    {
        return new CompanyDto(company.Id, company.Code, company.Name, company.TaxId, company.IsActive);
    }

    private static void ApplyBranch(Branch branch, SaveBranchRequest request)
    {
        branch.CompanyId = request.CompanyId;
        branch.Code = request.Code.Trim().ToUpperInvariant();
        branch.Name = request.Name.Trim();
        branch.TaxId = string.IsNullOrWhiteSpace(request.TaxId) ? null : request.TaxId.Trim();
        branch.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        branch.IsActive = request.IsActive;
    }

    private static BranchDto ToDto(Branch branch)
    {
        return new BranchDto(
            branch.Id,
            branch.CompanyId,
            branch.Company?.Code ?? string.Empty,
            branch.Company?.Name ?? string.Empty,
            branch.Code,
            branch.Name,
            branch.TaxId,
            branch.Address,
            branch.IsActive);
    }

    private static void ApplyCostCenter(CostCenter costCenter, SaveCostCenterRequest request)
    {
        costCenter.CompanyId = request.CompanyId;
        costCenter.Code = request.Code.Trim().ToUpperInvariant();
        costCenter.Name = request.Name.Trim();
        costCenter.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        costCenter.IsActive = request.IsActive;
    }

    private static CostCenterDto ToDto(CostCenter costCenter)
    {
        return new CostCenterDto(
            costCenter.Id,
            costCenter.CompanyId,
            costCenter.Company?.Code ?? string.Empty,
            costCenter.Company?.Name ?? string.Empty,
            costCenter.Code,
            costCenter.Name,
            costCenter.Description,
            costCenter.IsActive);
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
        warehouse.BranchId = request.BranchId;
        warehouse.IsActive = request.IsActive;
    }

    private static WarehouseDto ToDto(Warehouse warehouse)
    {
        return new WarehouseDto(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Location, warehouse.BranchId, warehouse.Branch?.Name, warehouse.IsActive);
    }

    private async Task ApplyCurrencyAsync(CurrencyUnit currency, SaveCurrencyUnitRequest request)
    {
        if (request.IsDefault)
        {
            await db.CurrencyUnits
                .Where(x => x.Id != currency.Id && x.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false));
        }

        currency.Code = request.Code.Trim().ToUpperInvariant();
        currency.Name = request.Name.Trim();
        currency.Symbol = request.Symbol.Trim();
        currency.IsDefault = request.IsDefault;
    }

    private static CurrencyUnitDto ToDto(CurrencyUnit currency)
    {
        return new CurrencyUnitDto(currency.Id, currency.Code, currency.Name, currency.Symbol, currency.IsDefault);
    }

    private async Task<string?> GetCompanyValidationErrorAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            return "Company is required.";
        }

        return await db.Companies.AnyAsync(x => x.Id == companyId)
            ? null
            : "Invalid company.";
    }

    private async Task<string?> GetBranchValidationErrorAsync(Guid? branchId)
    {
        if (!branchId.HasValue)
        {
            return null;
        }

        return await db.Branches.AnyAsync(x => x.Id == branchId)
            ? null
            : "Invalid branch.";
    }

    private async Task<string?> GetExchangeRateValidationErrorAsync(SaveExchangeRateRequest request, Guid? id = null)
    {
        if (request.RateDate == default)
        {
            return "Exchange rate date is required.";
        }

        if (request.FromCurrencyId == request.ToCurrencyId)
        {
            return "Source and target currencies must be different.";
        }

        var currencyCount = await db.CurrencyUnits
            .CountAsync(x => x.Id == request.FromCurrencyId || x.Id == request.ToCurrencyId);
        if (currencyCount != 2)
        {
            return "Invalid source or target currency.";
        }

        var duplicateExists = await db.ExchangeRates.AnyAsync(x =>
            x.Id != id &&
            x.FromCurrencyId == request.FromCurrencyId &&
            x.ToCurrencyId == request.ToCurrencyId &&
            x.RateDate == request.RateDate);
        return duplicateExists ? "An exchange rate already exists for this currency pair and date." : null;
    }

    private static void ApplyExchangeRate(ExchangeRate exchangeRate, SaveExchangeRateRequest request)
    {
        exchangeRate.FromCurrencyId = request.FromCurrencyId;
        exchangeRate.ToCurrencyId = request.ToCurrencyId;
        exchangeRate.RateDate = request.RateDate;
        exchangeRate.Rate = request.Rate;
        exchangeRate.Source = string.IsNullOrWhiteSpace(request.Source) ? null : request.Source.Trim();
    }

    private static ExchangeRateDto ToDto(ExchangeRate exchangeRate)
    {
        return new ExchangeRateDto(
            exchangeRate.Id,
            exchangeRate.FromCurrencyId,
            exchangeRate.FromCurrency?.Code ?? string.Empty,
            exchangeRate.ToCurrencyId,
            exchangeRate.ToCurrency?.Code ?? string.Empty,
            exchangeRate.RateDate,
            exchangeRate.Rate,
            exchangeRate.Source);
    }
}
