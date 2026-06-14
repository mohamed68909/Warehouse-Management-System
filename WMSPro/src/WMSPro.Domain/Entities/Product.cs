using WMSPro.Domain.Common;

namespace WMSPro.Domain.Entities;

public sealed class Product : AggregateRoot
{
    private Product() { }

    private Product(
        Guid id,
        string name,
        string sku,
        string category,
        decimal price,
        int quantity) : base(id)
    {
        Name = name;
        SKU = sku;
        Category = category;
        Price = price;
        Quantity = quantity;
    }

    public string Name { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }

    // Low stock threshold - can be made configurable later
    public bool IsLowStock => Quantity <= 20;

    // --- Factory Method ---
    public static Result<Product> Create(
        string name,
        string sku,
        string category,
        decimal price,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(Error.Validation(nameof(name), "Product name is required."));

        if (string.IsNullOrWhiteSpace(sku))
            return Result.Failure<Product>(Error.Validation(nameof(sku), "SKU is required."));

        if (price < 0)
            return Result.Failure<Product>(Error.Validation(nameof(price), "Price cannot be negative."));

        if (quantity < 0)
            return Result.Failure<Product>(Error.Validation(nameof(quantity), "Quantity cannot be negative."));

        var product = new Product(
            Guid.NewGuid(), name.Trim(), sku.Trim().ToUpper(),
            category.Trim(), price, quantity);

        return Result.Success(product);
    }

    // --- Domain Methods ---
    public Result Update(string name, string category, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Error.Validation(nameof(name), "Product name is required."));

        if (price < 0)
            return Result.Failure(Error.Validation(nameof(price), "Price cannot be negative."));

        Name = name.Trim();
        Category = category.Trim();
        Price = price;

        return Result.Success();
    }

    public Result AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity must be positive."));

        Quantity += quantity;
        return Result.Success();
    }

    public Result RemoveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity must be positive."));

        if (Quantity < quantity)
            return Result.Failure(Error.InsufficientStock(Name, Quantity, quantity));

        Quantity -= quantity;
        return Result.Success();
    }
}
