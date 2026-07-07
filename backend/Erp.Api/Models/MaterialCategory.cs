namespace Erp.Api.Models;

public sealed class MaterialCategory : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Material> Materials { get; set; } = [];
}
