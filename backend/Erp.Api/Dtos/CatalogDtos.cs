using System.ComponentModel.DataAnnotations;

namespace Erp.Api.Dtos;

public sealed record CatalogItemDto(Guid Id, string Name, string? Description);

public sealed record UnitOfMeasureDto(Guid Id, string Code, string Name);

public sealed record SaveCatalogItemRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(500)] string? Description);

public sealed record SaveUnitOfMeasureRequest(
    [Required, MaxLength(12)] string Code,
    [Required, MaxLength(80)] string Name);

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

public sealed record WarehouseDto(Guid Id, string Code, string Name, string? Location, bool IsActive);

public sealed record SaveWarehouseRequest(
    [Required, MaxLength(20)] string Code,
    [Required, MaxLength(120)] string Name,
    [MaxLength(200)] string? Location,
    bool IsActive);
