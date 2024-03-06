using Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers
{
    public class AuctionsCreatedConsumer : IConsumer<AuctionCreated>
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        public AuctionsCreatedConsumer(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<AuctionCreated> context)
        {
            Console.WriteLine("=>> auction created");

            await _hubContext.Clients.All.SendAsync("Auction Created", context.Message);
        }

    }
}
