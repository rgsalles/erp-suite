namespace Erp.Api.Models;

public sealed class FinancialEntry : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public FinancialEntryType Type { get; set; }
    public FinancialEntryStatus Status { get; set; } = FinancialEntryStatus.Open;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? SettledAt { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Description { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public Guid? SettledByUserId { get; set; }
    public AppUser? SettledByUser { get; set; }
}
