using Microsoft.AspNetCore.SignalR;

namespace LarvaX.Web.Hubs
{
    public class VideoConsultHub : Hub
    {
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(roomId));
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(roomId));
        }

        public async Task SendOffer(string roomId, string sdp)
        {
            await Clients.OthersInGroup(GetGroupName(roomId)).SendAsync("ReceiveOffer", Context.ConnectionId, sdp);
        }

        public async Task SendAnswer(string roomId, string sdp)
        {
            await Clients.OthersInGroup(GetGroupName(roomId)).SendAsync("ReceiveAnswer", Context.ConnectionId, sdp);
        }

        public async Task SendIceCandidate(string roomId, string candidate)
        {
            await Clients.OthersInGroup(GetGroupName(roomId)).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
        }

        private static string GetGroupName(string roomId) => $"VideoRoom_{roomId}";
    }
}
