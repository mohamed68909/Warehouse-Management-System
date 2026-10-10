namespace WMSPro.Mvc.Services;

public record LiveNotification(
    string Type,          // "Transaction", "Order", "Alert", "Bin"
    string Title,
    string Message,
    string Severity,      // "success", "info", "warning", "danger"
    DateTime Timestamp,
    object? Data = null);

public interface IWmsNotifier
{
    Task NotifyTransactionAsync(string type, string productName, int quantity, string warehouse, string reference);
    Task NotifyOrderStatusAsync(string orderNumber, string status, string orderType, decimal totalAmount);
    Task NotifyLowStockAsync(string productName, string sku, int remainingQuantity);
    Task NotifyBinAllocatedAsync(string locationCode, string productName, int quantity);
}
