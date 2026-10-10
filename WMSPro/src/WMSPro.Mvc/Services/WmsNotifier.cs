using Microsoft.AspNetCore.SignalR;
using WMSPro.Mvc.Hubs;

namespace WMSPro.Mvc.Services;

public class WmsNotifier : IWmsNotifier
{
    private readonly IHubContext<WmsHub> _hubContext;

    public WmsNotifier(IHubContext<WmsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTransactionAsync(string type, string productName, int quantity, string warehouse, string reference)
    {
        var severity = type.Equals("In", StringComparison.OrdinalIgnoreCase) ? "success" :
                       (type.Equals("Out", StringComparison.OrdinalIgnoreCase) ? "danger" : "info");

        var actionText = type.Equals("In", StringComparison.OrdinalIgnoreCase) ? "Stock IN Received" :
                         (type.Equals("Out", StringComparison.OrdinalIgnoreCase) ? "Stock OUT Dispatched" : "Stock Transferred");

        var notification = new LiveNotification(
            Type: "Transaction",
            Title: $"{actionText}: {productName}",
            Message: $"{quantity} units at {warehouse} (Ref: {reference})",
            Severity: severity,
            Timestamp: DateTime.UtcNow,
            Data: new
            {
                Type = type,
                ProductName = productName,
                Quantity = quantity,
                Warehouse = warehouse,
                Reference = reference,
                Time = DateTime.UtcNow
            });

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        await _hubContext.Clients.All.SendAsync("ReceiveTransaction", notification.Data);
    }

    public async Task NotifyOrderStatusAsync(string orderNumber, string status, string orderType, decimal totalAmount)
    {
        var severity = status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ? "success" :
                       (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ? "danger" : "primary");

        var notification = new LiveNotification(
            Type: "Order",
            Title: $"Order {orderNumber} &rarr; {status}",
            Message: $"{orderType} Order of value ${totalAmount:N2} marked as {status}.",
            Severity: severity,
            Timestamp: DateTime.UtcNow,
            Data: new { OrderNumber = orderNumber, Status = status, OrderType = orderType, TotalAmount = totalAmount });

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
    }

    public async Task NotifyLowStockAsync(string productName, string sku, int remainingQuantity)
    {
        var notification = new LiveNotification(
            Type: "Alert",
            Title: $"Critical Low Stock Warning: {productName}",
            Message: $"Only {remainingQuantity} units left in stock (SKU: {sku}). Reorder urgently.",
            Severity: "warning",
            Timestamp: DateTime.UtcNow,
            Data: new { ProductName = productName, SKU = sku, Quantity = remainingQuantity });

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
    }

    public async Task NotifyBinAllocatedAsync(string locationCode, string productName, int quantity)
    {
        var notification = new LiveNotification(
            Type: "Bin",
            Title: $"Storage Bin Updated: {locationCode}",
            Message: $"{quantity} units of {productName} allocated to bin {locationCode}.",
            Severity: "info",
            Timestamp: DateTime.UtcNow,
            Data: new { LocationCode = locationCode, ProductName = productName, Quantity = quantity });

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
    }
}
