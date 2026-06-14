using WMSPro.Domain.Common;
using WMSPro.Domain.Enums;

namespace WMSPro.Domain.Events;

/// <summary>
/// Raised when an Order status changes to Completed.
/// Triggers automatic InventoryTransaction creation in Application layer.
/// </summary>
public record OrderCompletedEvent(
    Guid OrderId,
    string OrderNumber,
    OrderType OrderType,
    Guid WarehouseId,
    IReadOnlyList<OrderItemSnapshot> Items) : IDomainEvent;

/// <summary>Snapshot of an order item at completion time (avoids lazy loading issues).</summary>
public record OrderItemSnapshot(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
