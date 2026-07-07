namespace Erp.Api.Models;

public enum UserRole
{
    Admin,
    Manager,
    Buyer,
    Seller,
    Stock,
    Operator
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    PartiallyReceived,
    Received,
    PartiallyShipped,
    Shipped,
    Cancelled
}

public enum StockMovementType
{
    Inbound,
    Outbound,
    Adjustment,
    PurchaseReceipt,
    SalesShipment
}

public enum FinancialEntryType
{
    Payable,
    Receivable
}

public enum FinancialEntryStatus
{
    Open,
    Paid,
    Cancelled
}
