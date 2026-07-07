namespace Erp.Api.Messaging;

public static class RabbitMqRoutingKeys
{
    public const string StockMovementCreated = "inventory.stock-movement.created";
    public const string PurchaseOrderReceived = "purchasing.order.received";
    public const string SalesOrderShipped = "sales.order.shipped";
    public const string FinancialEntryCreated = "finance.entry.created";
}
