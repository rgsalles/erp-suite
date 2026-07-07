using Erp.Api.Models;

namespace Erp.Api.Messaging;

public sealed record StockMovementCreatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid StockMovementId,
    Guid MaterialId,
    string MaterialCode,
    string MaterialDescription,
    Guid WarehouseId,
    string WarehouseCode,
    StockMovementType MovementType,
    decimal Quantity,
    decimal SignedQuantity,
    decimal CurrentStock,
    decimal MinimumStock,
    Guid? UserId,
    string? Reference);

public sealed record PurchaseOrderReceivedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid PurchaseOrderId,
    string Number,
    Guid SupplierId,
    Guid WarehouseId,
    Guid? UserId);

public sealed record SalesOrderShippedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid SalesOrderId,
    string Number,
    Guid CustomerId,
    Guid WarehouseId,
    Guid? UserId);

public sealed record FinancialEntryCreatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid FinancialEntryId,
    string Number,
    FinancialEntryType Type,
    FinancialEntryStatus Status,
    decimal Amount,
    DateTime DueDate,
    Guid? SupplierId,
    Guid? CustomerId,
    Guid? PurchaseOrderId,
    Guid? SalesOrderId);
