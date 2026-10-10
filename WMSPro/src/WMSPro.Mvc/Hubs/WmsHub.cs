using Microsoft.AspNetCore.SignalR;

namespace WMSPro.Mvc.Hubs;

public class WmsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public async Task JoinWarehouseChannel(string warehouseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"warehouse-{warehouseId}");
    }

    public async Task LeaveWarehouseChannel(string warehouseId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"warehouse-{warehouseId}");
    }
}
