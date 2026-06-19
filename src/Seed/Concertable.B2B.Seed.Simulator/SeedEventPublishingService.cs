using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Simulator;

internal sealed class SeedEventPublishingService : BackgroundService
{
    private readonly IBusTransport transport;
    private readonly SeedCatalog fixture;
    private readonly IHostApplicationLifetime lifetime;
    private readonly ILogger<SeedEventPublishingService> logger;

    public SeedEventPublishingService(
        IBusTransport transport,
        SeedCatalog fixture,
        IHostApplicationLifetime lifetime,
        ILogger<SeedEventPublishingService> logger)
    {
        this.transport = transport;
        this.fixture = fixture;
        this.lifetime = lifetime;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stand in for B2B's TenantProvisioningHandler (absent when real B2B isn't running) so Payment
        // provisions each operator's Connect account off the same TenantCreatedEvent the prod path emits.
        foreach (var m in SeedUsers.Managers)
            await transport.PublishAsync(
                new TenantCreatedEvent(m.TenantId, m.Id, m.Email),
                Envelope(typeof(TenantCreatedEvent)), stoppingToken);
        logger.PublishedTenantEvents(SeedUsers.Managers.Count());

        foreach (var v in fixture.Venues)
            await transport.PublishAsync(v.ToChangedEvent(), Envelope(typeof(Concertable.B2B.Venue.Contracts.Events.VenueChangedEvent)), stoppingToken);
        logger.PublishedVenueEvents(fixture.Venues.Count);

        foreach (var a in fixture.Artists)
            await transport.PublishAsync(a.ToChangedEvent(), Envelope(typeof(Concertable.B2B.Artist.Contracts.Events.ArtistChangedEvent)), stoppingToken);
        logger.PublishedArtistEvents(fixture.Artists.Count);

        foreach (var c in fixture.Concerts)
            await transport.PublishAsync(c.ToChangedEvent(), Envelope(typeof(Concertable.B2B.Concert.Contracts.Events.ConcertChangedEvent)), stoppingToken);
        logger.PublishedConcertEvents(fixture.Concerts.Count);

        lifetime.StopApplication();
    }

    private static MessageEnvelope Envelope(Type eventType) =>
        new(Guid.NewGuid(), MessageTypeAttribute.Resolve(eventType), DateTimeOffset.UtcNow);
}
