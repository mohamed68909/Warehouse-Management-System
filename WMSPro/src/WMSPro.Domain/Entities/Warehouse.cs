using WMSPro.Domain.Common;

namespace WMSPro.Domain.Entities;

public sealed class Warehouse : AggregateRoot
{
    // Private constructor - use Create() factory method
    private Warehouse() { }

    private Warehouse(
        Guid id,
        string name,
        int capacity,
        string location,
        string managerName,
        string phone,
        string email) : base(id)
    {
        Name = name;
        Capacity = capacity;
        Location = location;
        ManagerName = managerName;
        Phone = phone;
        Email = email;
    }

    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string ManagerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    // Computed property - current used units (updated via Inventory)
    public int CurrentStock { get; private set; }

    public double CapacityUsagePercent =>
        Capacity > 0 ? Math.Round((double)CurrentStock / Capacity * 100, 1) : 0;

    // --- Factory Method ---
    public static Result<Warehouse> Create(
        string name,
        int capacity,
        string location,
        string managerName,
        string phone,
        string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Warehouse>(Error.Validation(nameof(name), "Warehouse name is required."));

        if (capacity <= 0)
            return Result.Failure<Warehouse>(Error.Validation(nameof(capacity), "Capacity must be greater than zero."));

        if (string.IsNullOrWhiteSpace(location))
            return Result.Failure<Warehouse>(Error.Validation(nameof(location), "Location is required."));

        var warehouse = new Warehouse(
            Guid.NewGuid(), name.Trim(), capacity,
            location.Trim(), managerName.Trim(), phone.Trim(), email.Trim());

        return Result.Success(warehouse);
    }

    // --- Domain Methods ---
    public Result Update(string name, int capacity, string location, string managerName, string phone, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Error.Validation(nameof(name), "Warehouse name is required."));

        if (capacity <= 0)
            return Result.Failure(Error.Validation(nameof(capacity), "Capacity must be greater than zero."));

        if (capacity < CurrentStock)
            return Result.Failure(Error.Validation(nameof(capacity),
                $"New capacity ({capacity}) cannot be less than current stock ({CurrentStock})."));

        Name = name.Trim();
        Capacity = capacity;
        Location = location.Trim();
        ManagerName = managerName.Trim();
        Phone = phone.Trim();
        Email = email.Trim();

        return Result.Success();
    }

    public Result AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity must be positive."));

        if (CurrentStock + quantity > Capacity)
            return Result.Failure(Error.Conflict(
                $"Adding {quantity} units would exceed warehouse capacity of {Capacity}."));

        CurrentStock += quantity;
        return Result.Success();
    }

    public Result RemoveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(nameof(quantity), "Quantity must be positive."));

        if (CurrentStock - quantity < 0)
            return Result.Failure(Error.Conflict(
                $"Cannot remove {quantity} units. Only {CurrentStock} available."));

        CurrentStock -= quantity;
        return Result.Success();
    }
}
