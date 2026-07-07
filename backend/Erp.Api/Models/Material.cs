namespace Erp.Api.Models;

public sealed class Material : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public MaterialCategory? Category { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal StandardCost { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<StockMovement> StockMovements { get; set; } = [];
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = [];
    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = [];
}
