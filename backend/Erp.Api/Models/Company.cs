namespace Erp.Api.Models;

public sealed class Company : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Branch> Branches { get; set; } = [];
    public ICollection<CostCenter> CostCenters { get; set; } = [];
}
