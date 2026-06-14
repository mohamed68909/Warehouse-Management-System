using WMSPro.Domain.Common;
using WMSPro.Domain.Enums;
using WMSPro.Domain.Events;

namespace WMSPro.Domain.Entities;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    private Order(
        Guid id,
        string orderNumber,
        OrderType type,
        string supplierCustomerName,
        Guid warehouseId,
        DateTime date) : base(id)
    {
        OrderNumber = orderNumber;
        Type = type;
        SupplierCustomerName = supplierCustomerName;
        WarehouseId = warehouseId;
        Status = OrderStatus.Pending;
        Date = date;
    }

    public string OrderNumber { get; private set; } = string.Empty;
    public OrderType Type { get; private set; }
    public OrderStatus Status { get; private set; }
    public string SupplierCustomerName { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public DateTime Date { get; private set; }
    public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // Navigation property
    public Warehouse? Warehouse { get; private set; }

    // ─── Factory Method ────────────────────────────────────────────────────────
    public static Result<Order> Create(
        string orderNumber,
        OrderType type,
        string supplierCustomerName,
        Guid warehouseId,
        DateTime? date = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return Result.Failure<Order>(Error.Validation(nameof(orderNumber), "Order number is required."));

        if (string.IsNullOrWhiteSpace(supplierCustomerName))
            return Result.Failure<Order>(Error.Validation(nameof(supplierCustomerName), "Supplier/Customer name is required."));

        if (warehouseId == Guid.Empty)
            return Result.Failure<Order>(Error.Validation(nameof(warehouseId), "Warehouse is required."));

        var order = new Order(Guid.NewGuid(), orderNumber.Trim(), type,
            supplierCustomerName.Trim(), warehouseId, date ?? DateTime.UtcNow);

        return Result.Success(order);
    }

    // ─── Domain Methods ────────────────────────────────────────────────────────

    /// <summary>Add an item to the order. Only possible before completion/cancellation.</summary>
    public Result AddOrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            return Result.Failure(Error.Conflict($"Cannot modify a {Status} order."));

        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity must be positive."));

        if (unitPrice < 0)
            return Result.Failure(Error.Validation(nameof(unitPrice), "Unit price cannot be negative."));

        // Merge if product already added
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            _items.Remove(existing);
            _items.Add(new OrderItem(existing.Id, Id, productId, productName,
                existing.Quantity + quantity, unitPrice));
        }
        else
        {
            _items.Add(new OrderItem(Guid.NewGuid(), Id, productId, productName, quantity, unitPrice));
        }

        return Result.Success();
    }

    /// <summary>
    /// Changes the order status. Fires OrderCompletedEvent when status → Completed.
    /// </summary>
    public Result UpdateStatus(OrderStatus newStatus)
    {
        if (Status == newStatus)
            return Result.Failure(Error.Conflict($"Order is already in '{newStatus}' status."));

        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            return Result.Failure(Error.Conflict($"Cannot change status of a '{Status}' order."));

        if (!_items.Any() && newStatus == OrderStatus.Completed)
            return Result.Failure(Error.Validation("Items", "Cannot complete an order with no items."));

        Status = newStatus;

        // ── Fire Domain Event when completed ──────────────────────────────────
        if (newStatus == OrderStatus.Completed)
        {
            var snapshots = _items
                .Select(i => new OrderItemSnapshot(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
                .ToList()
                .AsReadOnly();

            RaiseDomainEvent(new OrderCompletedEvent(Id, OrderNumber, Type, WarehouseId, snapshots));
        }

        return Result.Success();
    }
}
