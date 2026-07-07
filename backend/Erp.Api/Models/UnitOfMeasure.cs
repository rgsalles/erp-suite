namespace Erp.Api.Models;

public sealed class UnitOfMeasure : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Material> Materials { get; set; } = [];
}
