using System.ComponentModel.DataAnnotations;

namespace Erp.Api.Dtos;

public sealed record MaterialDto(
    Guid Id,
    string Code,
    string Description,
    Guid CategoryId,
    string CategoryName,
    Guid UnitOfMeasureId,
    string UnitCode,
    Guid? SupplierId,
    string? SupplierName,
    decimal StandardCost,
    decimal SalePrice,
    decimal MinimumStock,
    decimal CurrentStock,
    bool IsActive);

public sealed record SaveMaterialRequest(
    [Required, MaxLength(40)] string Code,
    [Required, MaxLength(240)] string Description,
    Guid CategoryId,
    Guid UnitOfMeasureId,
    Guid? SupplierId,
    [Range(0, 999999999)] decimal StandardCost,
    [Range(0, 999999999)] decimal SalePrice,
    [Range(0, 999999999)] decimal MinimumStock,
    bool IsActive);
