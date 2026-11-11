using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Simulator;

internal sealed class SeedEventPublisher : BackgroundService
{
    private readonly IBusTransport busTransport;
    private readonly SeedCatalog seedCatalog;
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly ILogger<SeedEventPublisher> logger;

    public SeedEventPublisher(
        IBusTransport busTransport,
        SeedCatalog seedCatalog,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<SeedEventPublisher> logger)
    {
        this.busTransport = busTransport;
        this.seedCatalog = seedCatalog;
        this.hostApplicationLifetime = hostApplicationLifetime;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stand in for B2B's TenantProvisioningHandler (absent when real B2B isn't running) so Payment
        // provisions each operator's Connect account off the same events the prod path emits.
        foreach (var manager in SeedUsers.Managers)
        {
            await busTransport.PublishAsync(
                new PayoutOwnerRegisteredEvent(manager.TenantId, manager.Email),
                CreateEnvelope(typeof(PayoutOwnerRegisteredEvent)), stoppingToken);
        }
        logger.PublishedPayoutOwnerRegistrations(SeedUsers.Managers.Count());

        foreach (var venue in seedCatalog.Venues)
            await busTransport.PublishAsync(
                venue.ToChangedEvent(), CreateEnvelope(typeof(VenueChangedEvent)), stoppingToken);
        logger.PublishedVenueEvents(seedCatalog.Venues.Count);

        foreach (var artist in seedCatalog.Artists)
            await busTransport.PublishAsync(
                artist.ToChangedEvent(), CreateEnvelope(typeof(ArtistChangedEvent)), stoppingToken);
        logger.PublishedArtistEvents(seedCatalog.Artists.Count);

        foreach (var concert in seedCatalog.Concerts)
            await busTransport.PublishAsync(
                concert.ToChangedEvent(), CreateEnvelope(typeof(ConcertChangedEvent)), stoppingToken);
        logger.PublishedConcertEvents(seedCatalog.Concerts.Count);

        hostApplicationLifetime.StopApplication();
    }

    private static MessageEnvelope CreateEnvelope(Type eventType) =>
        new(Guid.NewGuid(), MessageTypeAttribute.Resolve(eventType), DateTimeOffset.UtcNow);
}
