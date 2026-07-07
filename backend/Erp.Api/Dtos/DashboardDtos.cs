namespace Erp.Api.Dtos;

public sealed record DashboardDto(
    int ActiveMaterials,
    int ActiveCustomers,
    int ActiveSuppliers,
    int OpenPurchaseOrders,
    int OpenSalesOrders,
    int LowStockMaterials,
    decimal InventoryValue,
    decimal OpenPayables,
    decimal OpenReceivables,
    int OverdueFinancialEntries,
    IReadOnlyCollection<StockBalanceDto> LowStockItems);
