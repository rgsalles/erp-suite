using System.ComponentModel.DataAnnotations;
using Erp.Api.Models;

namespace Erp.Api.Dtos;

public sealed record PurchaseOrderDto(
    Guid Id,
    string Number,
    Guid SupplierId,
    string SupplierName,
    OrderStatus Status,
    DateTime OrderDate,
    DateTime? ExpectedDate,
    DateTime? ReceivedAt,
    string? Notes,
    decimal Total,
    IReadOnlyCollection<PurchaseOrderItemDto> Items);

public sealed record PurchaseOrderItemDto(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    string MaterialDescription,
    decimal Quantity,
    decimal UnitCost,
    decimal ReceivedQuantity,
    decimal LineTotal);

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    DateTime? ExpectedDate,
    [MaxLength(500)] string? Notes,
    [MinLength(1)] IReadOnlyCollection<CreatePurchaseOrderItemRequest> Items);

public sealed record CreatePurchaseOrderItemRequest(
    Guid MaterialId,
    [Range(0.001, 999999999)] decimal Quantity,
    [Range(0, 999999999)] decimal UnitCost);

public sealed record ReceivePurchaseOrderRequest(Guid WarehouseId);

public sealed record SalesOrderDto(
    Guid Id,
    string Number,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    DateTime OrderDate,
    DateTime? ShippedAt,
    string? Notes,
    decimal Total,
    IReadOnlyCollection<SalesOrderItemDto> Items);

public sealed record SalesOrderItemDto(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    string MaterialDescription,
    decimal Quantity,
    decimal UnitPrice,
    decimal ShippedQuantity,
    decimal LineTotal);

public sealed record CreateSalesOrderRequest(
    Guid CustomerId,
    [MaxLength(500)] string? Notes,
    [MinLength(1)] IReadOnlyCollection<CreateSalesOrderItemRequest> Items);

public sealed record CreateSalesOrderItemRequest(
    Guid MaterialId,
    [Range(0.001, 999999999)] decimal Quantity,
    [Range(0, 999999999)] decimal UnitPrice);

public sealed record ShipSalesOrderRequest(Guid WarehouseId);
