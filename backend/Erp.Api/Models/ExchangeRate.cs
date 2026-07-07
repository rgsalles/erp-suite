namespace Erp.Api.Models;

public sealed class ExchangeRate : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromCurrencyId { get; set; }
    public CurrencyUnit? FromCurrency { get; set; }
    public Guid ToCurrencyId { get; set; }
    public CurrencyUnit? ToCurrency { get; set; }
    public DateOnly RateDate { get; set; }
    public decimal Rate { get; set; }
    public string? Source { get; set; }
}
