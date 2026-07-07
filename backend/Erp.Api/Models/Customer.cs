namespace Erp.Api.Models;

public sealed class Customer : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ContactName { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SalesOrder> SalesOrders { get; set; } = [];
}
