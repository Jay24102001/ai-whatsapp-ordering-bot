using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AiBotOrderingSystem.Hubs
{
    public class OrderHub : Hub
    {
        // Server can call Clients.All.SendAsync("NewOrder", orderDto) or
        // Clients.All.SendAsync("OrderUpdated", orderDto)
        public Task Hello() => Clients.Caller.SendAsync("Hello", "connected");
    }
}