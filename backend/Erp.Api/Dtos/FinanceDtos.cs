using System.ComponentModel.DataAnnotations;
using Erp.Api.Models;

namespace Erp.Api.Dtos;

public sealed record FinancialEntryDto(
    Guid Id,
    string Number,
    FinancialEntryType Type,
    FinancialEntryStatus Status,
    DateTime IssueDate,
    DateTime DueDate,
    DateTime? SettledAt,
    decimal Amount,
    decimal PaidAmount,
    decimal OpenAmount,
    bool IsOverdue,
    string? Description,
    Guid? SupplierId,
    string? SupplierName,
    Guid? CustomerId,
    string? CustomerName,
    Guid? PurchaseOrderId,
    string? PurchaseOrderNumber,
    Guid? SalesOrderId,
    string? SalesOrderNumber);

public sealed record FinancialSummaryDto(
    decimal OpenPayables,
    decimal OverduePayables,
    decimal OpenReceivables,
    decimal OverdueReceivables,
    decimal PaidThisMonth,
    decimal ReceivedThisMonth,
    decimal NetCashFlowThisMonth,
    IReadOnlyCollection<FinancialEntryDto> NextPayables,
    IReadOnlyCollection<FinancialEntryDto> NextReceivables);

public sealed record CreateFinancialEntryRequest(
    FinancialEntryType Type,
    DateTime DueDate,
    [Range(0.01, 999999999)] decimal Amount,
    [MaxLength(500)] string? Description,
    Guid? SupplierId,
    Guid? CustomerId);

public sealed record SettleFinancialEntryRequest(
    DateTime? SettledAt,
    [Range(0.01, 999999999)] decimal? PaidAmount);
