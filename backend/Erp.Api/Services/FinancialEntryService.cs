using Erp.Api.Data;
using Erp.Api.Messaging;
using Erp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Services;

public sealed class FinancialEntryService(ErpDbContext db, IIntegrationEventPublisher eventPublisher)
{
    public async Task<FinancialEntry?> CreatePayableForPurchaseOrderAsync(Guid purchaseOrderId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var order = await db.PurchaseOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var total = order.Items.Sum(x => x.Quantity * x.UnitCost);
        if (total <= 0)
        {
            return null;
        }

        var exists = await db.FinancialEntries.AnyAsync(
            x => x.Type == FinancialEntryType.Payable
                && x.PurchaseOrderId == purchaseOrderId
                && x.Status != FinancialEntryStatus.Cancelled,
            cancellationToken);

        if (exists)
        {
            return null;
        }

        var entry = new FinancialEntry
        {
            Number = await GenerateNumberAsync(FinancialEntryType.Payable, cancellationToken),
            Type = FinancialEntryType.Payable,
            Status = FinancialEntryStatus.Open,
            IssueDate = DateTime.UtcNow,
            DueDate = (order.ExpectedDate ?? DateTime.UtcNow.AddDays(30)).Date,
            Amount = total,
            PaidAmount = 0,
            SupplierId = order.SupplierId,
            PurchaseOrderId = order.Id,
            Description = $"Conta a pagar gerada pelo recebimento do pedido {order.Number}.",
            CreatedByUserId = userId
        };

        db.FinancialEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await PublishCreatedEventAsync(entry, cancellationToken);

        return entry;
    }

    public async Task<FinancialEntry?> CreateReceivableForSalesOrderAsync(Guid salesOrderId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var order = await db.SalesOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var total = order.Items.Sum(x => x.Quantity * x.UnitPrice);
        if (total <= 0)
        {
            return null;
        }

        var exists = await db.FinancialEntries.AnyAsync(
            x => x.Type == FinancialEntryType.Receivable
                && x.SalesOrderId == salesOrderId
                && x.Status != FinancialEntryStatus.Cancelled,
            cancellationToken);

        if (exists)
        {
            return null;
        }

        var entry = new FinancialEntry
        {
            Number = await GenerateNumberAsync(FinancialEntryType.Receivable, cancellationToken),
            Type = FinancialEntryType.Receivable,
            Status = FinancialEntryStatus.Open,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            Amount = total,
            PaidAmount = 0,
            CustomerId = order.CustomerId,
            SalesOrderId = order.Id,
            Description = $"Conta a receber gerada pela expedicao do pedido {order.Number}.",
            CreatedByUserId = userId
        };

        db.FinancialEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await PublishCreatedEventAsync(entry, cancellationToken);

        return entry;
    }

    public async Task<FinancialEntry> CreateManualAsync(
        FinancialEntryType type,
        DateTime dueDate,
        decimal amount,
        string? description,
        Guid? supplierId,
        Guid? customerId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (type == FinancialEntryType.Payable && supplierId is null)
        {
            throw new InvalidOperationException("A conta a pagar precisa de um fornecedor.");
        }

        if (type == FinancialEntryType.Receivable && customerId is null)
        {
            throw new InvalidOperationException("A conta a receber precisa de um cliente.");
        }

        if (supplierId.HasValue && !await db.Suppliers.AnyAsync(x => x.Id == supplierId.Value && x.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Fornecedor invalido ou inativo.");
        }

        if (customerId.HasValue && !await db.Customers.AnyAsync(x => x.Id == customerId.Value && x.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Cliente invalido ou inativo.");
        }

        var entry = new FinancialEntry
        {
            Number = await GenerateNumberAsync(type, cancellationToken),
            Type = type,
            Status = FinancialEntryStatus.Open,
            IssueDate = DateTime.UtcNow,
            DueDate = dueDate.Date,
            Amount = amount,
            PaidAmount = 0,
            Description = description,
            SupplierId = type == FinancialEntryType.Payable ? supplierId : null,
            CustomerId = type == FinancialEntryType.Receivable ? customerId : null,
            CreatedByUserId = userId
        };

        db.FinancialEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await PublishCreatedEventAsync(entry, cancellationToken);

        return entry;
    }

    public async Task<FinancialEntry?> SettleAsync(Guid id, DateTime? settledAt, decimal? paidAmount, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entry = await db.FinancialEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        if (entry.Status == FinancialEntryStatus.Cancelled)
        {
            throw new InvalidOperationException("Lancamento financeiro cancelado nao pode ser baixado.");
        }

        if (entry.Status == FinancialEntryStatus.Paid)
        {
            throw new InvalidOperationException("Lancamento financeiro ja foi baixado.");
        }

        var amountToSettle = paidAmount ?? entry.Amount;
        if (amountToSettle <= 0 || amountToSettle > entry.Amount)
        {
            throw new InvalidOperationException("Valor de baixa invalido.");
        }

        entry.Status = FinancialEntryStatus.Paid;
        entry.SettledAt = settledAt ?? DateTime.UtcNow;
        entry.PaidAmount = amountToSettle;
        entry.SettledByUserId = userId;
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<FinancialEntry?> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await db.FinancialEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        if (entry.Status == FinancialEntryStatus.Paid)
        {
            throw new InvalidOperationException("Lancamento financeiro ja baixado nao pode ser cancelado.");
        }

        entry.Status = FinancialEntryStatus.Cancelled;
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    private async Task<string> GenerateNumberAsync(FinancialEntryType type, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var prefix = type == FinancialEntryType.Payable ? "AP" : "AR";
        var count = await db.FinancialEntries.CountAsync(x => x.IssueDate >= today && x.IssueDate < tomorrow && x.Type == type, cancellationToken);

        return $"{prefix}-{today:yyyyMMdd}-{count + 1:0000}";
    }

    private Task PublishCreatedEventAsync(FinancialEntry entry, CancellationToken cancellationToken)
    {
        return eventPublisher.PublishAsync(
            RabbitMqRoutingKeys.FinancialEntryCreated,
            new FinancialEntryCreatedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTime.UtcNow,
                FinancialEntryId: entry.Id,
                Number: entry.Number,
                Type: entry.Type,
                Status: entry.Status,
                Amount: entry.Amount,
                DueDate: entry.DueDate,
                SupplierId: entry.SupplierId,
                CustomerId: entry.CustomerId,
                PurchaseOrderId: entry.PurchaseOrderId,
                SalesOrderId: entry.SalesOrderId),
            cancellationToken);
    }
}
