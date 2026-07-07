using System.ComponentModel.DataAnnotations;

namespace Erp.Api.Dtos;

public sealed record CatalogItemDto(Guid Id, string Name, string? Description);

public sealed record CompanyDto(Guid Id, string Code, string Name, string? TaxId, bool IsActive);

public sealed record BranchDto(
    Guid Id,
    Guid CompanyId,
    string CompanyCode,
    string CompanyName,
    string Code,
    string Name,
    string? TaxId,
    string? Address,
    bool IsActive);

public sealed record CostCenterDto(
    Guid Id,
    Guid CompanyId,
    string CompanyCode,
    string CompanyName,
    string Code,
    string Name,
    string? Description,
    bool IsActive);

public sealed record UnitOfMeasureDto(Guid Id, string Code, string Name);

public sealed record CurrencyUnitDto(Guid Id, string Code, string Name, string Symbol, bool IsDefault);

public sealed record ExchangeRateDto(
    Guid Id,
    Guid FromCurrencyId,
    string FromCurrencyCode,
    Guid ToCurrencyId,
    string ToCurrencyCode,
    DateOnly RateDate,
    decimal Rate,
    string? Source);

public sealed record SaveCatalogItemRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(500)] string? Description);

public sealed record SaveCompanyRequest(
    [Required, MaxLength(20)] string Code,
    [Required, MaxLength(160)] string Name,
    [MaxLength(40)] string? TaxId,
    bool IsActive);

public sealed record SaveBranchRequest(
    Guid CompanyId,
    [Required, MaxLength(20)] string Code,
    [Required, MaxLength(160)] string Name,
    [MaxLength(40)] string? TaxId,
    [MaxLength(240)] string? Address,
    bool IsActive);

public sealed record SaveCostCenterRequest(
    Guid CompanyId,
    [Required, MaxLength(30)] string Code,
    [Required, MaxLength(120)] string Name,
    [MaxLength(300)] string? Description,
    bool IsActive);

public sealed record SaveUnitOfMeasureRequest(
    [Required, MaxLength(12)] string Code,
    [Required, MaxLength(80)] string Name);

public sealed record SaveCurrencyUnitRequest(
    [Required, MinLength(3), MaxLength(3)] string Code,
    [Required, MaxLength(80)] string Name,
    [Required, MaxLength(8)] string Symbol,
    bool IsDefault);

public sealed record SaveExchangeRateRequest(
    Guid FromCurrencyId,
    Guid ToCurrencyId,
    DateOnly RateDate,
    [Range(0.00000001, 999999999)] decimal Rate,
    [MaxLength(120)] string? Source);

public sealed record BusinessPartnerDto(
    Guid Id,
    string Name,
    string TaxId,
    string? Email,
    string? Phone,
    string? ContactName,
    bool IsActive);

public sealed record SaveBusinessPartnerRequest(
    [Required, MaxLength(160)] string Name,
    [MaxLength(40)] string TaxId,
    [EmailAddress, MaxLength(200)] string? Email,
    [MaxLength(40)] string? Phone,
    [MaxLength(120)] string? ContactName,
    bool IsActive);

public sealed record WarehouseDto(Guid Id, string Code, string Name, string? Location, Guid? BranchId, string? BranchName, bool IsActive);

public sealed record SaveWarehouseRequest(
    [Required, MaxLength(20)] string Code,
    [Required, MaxLength(120)] string Name,
    [MaxLength(200)] string? Location,
    Guid? BranchId,
    bool IsActive);
