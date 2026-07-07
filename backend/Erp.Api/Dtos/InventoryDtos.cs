using System.ComponentModel.DataAnnotations;
using Erp.Api.Models;

namespace Erp.Api.Dtos;

public sealed record StockBalanceDto(
    Guid MaterialId,
    string MaterialCode,
    string MaterialDescription,
    string UnitCode,
    decimal MinimumStock,
    decimal CurrentStock,
    decimal InventoryValue,
    bool BelowMinimum);

public sealed record StockMovementDto(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    string MaterialDescription,
    Guid WarehouseId,
    string WarehouseCode,
    StockMovementType Type,
    decimal Quantity,
    decimal SignedQuantity,
    decimal? UnitCost,
    string? Reference,
    string? Notes,
    DateTime MovementDate);

public sealed record CreateStockMovementRequest(
    Guid MaterialId,
    Guid WarehouseId,
    StockMovementType Type,
    [Range(0.001, 999999999)] decimal Quantity,
    [Range(0, 999999999)] decimal? UnitCost,
    [MaxLength(80)] string? Reference,
    [MaxLength(500)] string? Notes);
