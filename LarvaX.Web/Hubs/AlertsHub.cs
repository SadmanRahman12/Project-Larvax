using Microsoft.AspNetCore.SignalR;

namespace LarvaX.Web.Hubs
{
    public class AlertsHub : Hub
    {
        // Clients can subscribe to specific zones if needed, or receive broadcast alerts
        public async Task JoinZoneGroup(string zoneId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
        }

        public async Task LeaveZoneGroup(string zoneId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
        }
    }
}
