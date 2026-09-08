using Microsoft.AspNetCore.SignalR;

namespace Meeting_Project.Hubs
{
    public class FlightHub : Hub
    {
        // frontend connect olacaq
        public async Task Join()
        {
            await Clients.Caller.SendAsync("connected", "Flight tracking started");
        }
    }
}