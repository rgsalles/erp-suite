using Erp.Api.Models;

namespace Erp.Api.Services;

public static class InventoryMath
{
    public static decimal SignedQuantity(StockMovementType type, decimal quantity)
    {
        return type is StockMovementType.Outbound or StockMovementType.SalesShipment ? -quantity : quantity;
    }
}
