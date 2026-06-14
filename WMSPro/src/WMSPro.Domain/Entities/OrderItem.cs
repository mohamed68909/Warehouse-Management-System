using WMSPro.Domain.Common;

namespace WMSPro.Domain.Entities;

/// <summary>
/// Owned entity - part of the Order aggregate.
/// Cannot exist without an Order.
/// </summary>
public sealed class OrderItem : Entity
{
    private OrderItem() { }

    internal OrderItem(Guid id, Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}
