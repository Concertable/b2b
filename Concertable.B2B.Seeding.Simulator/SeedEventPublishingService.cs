using Concertable.B2B.Seeding.Fixture;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Seeding.Simulator;

internal sealed class SeedEventPublishingService : BackgroundService
{
    private readonly IBusTransport transport;
    private readonly B2BSeedFixture fixture;
    private readonly IHostApplicationLifetime lifetime;
    private readonly ILogger<SeedEventPublishingService> logger;

    public SeedEventPublishingService(
        IBusTransport transport,
        B2BSeedFixture fixture,
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
        foreach (var v in fixture.Venues)
            await transport.PublishAsync(v.ToChangedEvent(), Envelope(typeof(Concertable.B2B.Venue.Contracts.Events.VenueChangedEvent)), stoppingToken);
        logger.LogInformation("Published {Count} venue events", fixture.Venues.Count);

        foreach (var a in fixture.Artists)
            await transport.PublishAsync(a.ToChangedEvent(), Envelope(typeof(Concertable.B2B.Artist.Contracts.Events.ArtistChangedEvent)), stoppingToken);
        logger.LogInformation("Published {Count} artist events", fixture.Artists.Count);

        foreach (var c in fixture.Concerts)
            await transport.PublishAsync(c.ToChangedEvent(), Envelope(typeof(Concertable.B2B.Concert.Contracts.Events.ConcertChangedEvent)), stoppingToken);
        logger.LogInformation("Published {Count} concert events", fixture.Concerts.Count);

        lifetime.StopApplication();
    }

    private static MessageEnvelope Envelope(Type eventType) =>
        new(Guid.NewGuid(), MessageTypeAttribute.Resolve(eventType), DateTimeOffset.UtcNow);
}
