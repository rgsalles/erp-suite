namespace Erp.Api.Models;

public sealed class PurchaseOrder : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}
