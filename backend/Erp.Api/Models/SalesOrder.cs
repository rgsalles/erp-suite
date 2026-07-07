namespace Erp.Api.Models;

public sealed class SalesOrder : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ShippedAt { get; set; }
    public string? Notes { get; set; }
    public ICollection<SalesOrderItem> Items { get; set; } = [];
}
