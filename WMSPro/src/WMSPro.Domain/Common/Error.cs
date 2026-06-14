namespace WMSPro.Domain.Common;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.");

    public static Error Validation(string field, string message) =>
        new($"Validation.{field}", message);

    public static Error Conflict(string message) =>
        new("Error.Conflict", message);

    public static Error InsufficientStock(string productName, int available, int requested) =>
        new("Inventory.InsufficientStock",
            $"Insufficient stock for '{productName}'. Available: {available}, Requested: {requested}.");
}
