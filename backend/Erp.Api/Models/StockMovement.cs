namespace Erp.Api.Models;

public sealed class StockMovement : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
}
