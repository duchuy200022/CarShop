using BiddingService.Models;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService.Services
{
    public class CheckAuctionFinished : BackgroundService
    {
        private readonly ILogger<CheckAuctionFinished> _logger;
        private readonly IServiceProvider _service;

        public CheckAuctionFinished(ILogger<CheckAuctionFinished> logger, IServiceProvider service)
        {
            _logger = logger;

    _service = service;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting check finished auctions");
            stoppingToken.Register(() => _logger.LogInformation("=>> Auction check Stopping"));

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAuctions(stoppingToken);
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task CheckAuctions(CancellationToken stoppingToken)
        {
            var finishedAuctions = await DB.Find<Auction>()
                .Match(x => x.AuctionEnd < DateTime.UtcNow)
                .Match(x => !x.Finished)
                .ExecuteAsync(stoppingToken);

            if (finishedAuctions.Count == 0)
                return;

            _logger.LogInformation($"=>>Found {finishedAuctions.Count} auctions finished");
            using var scope = _service.CreateScope();
            var endpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            foreach (var auction in finishedAuctions)
            {
                auction.Finished = true;
                await auction.SaveAsync(null, stoppingToken);

                var winningBid = await DB.Find<Bid>()
                    .Match(x => x.AuctionId == auction.ID)
                    .Match(y => y.BidStatus == BidStatus.Accepted)
                    .Sort(z => z.Descending(s => s.Amount))
                    .ExecuteFirstAsync(stoppingToken);
                await endpoint.Publish(
                    new AuctionFinished
                    {
                        ItemSold = winningBid != null,
                        AuctionId = auction.ID,
                        Winner = winningBid?.Bidder,
                        Amount = winningBid?.Amount,
                        Seller = auction.Seller
                    },
                    stoppingToken
                );
            }
        }
    }
}
