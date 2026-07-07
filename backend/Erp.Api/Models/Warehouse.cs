namespace Erp.Api.Models;

public sealed class Warehouse : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
