using Concertable.B2B.Seeding.Fixture;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Seeding.Simulator;

internal sealed class SeedEventPublishingService : BackgroundService
{
    private readonly IBusTransport transport;
    private readonly IHostApplicationLifetime lifetime;
    private readonly ILogger<SeedEventPublishingService> logger;

    public SeedEventPublishingService(
        IBusTransport transport,
        IHostApplicationLifetime lifetime,
        ILogger<SeedEventPublishingService> logger)
    {
        this.transport = transport;
        this.lifetime = lifetime;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        foreach (var v in B2BSeedFixture.Venues)
            await transport.PublishAsync(v, Envelope(typeof(Concertable.B2B.Venue.Contracts.Events.VenueChangedEvent)), stoppingToken);
        logger.LogInformation("Published {Count} venue events", B2BSeedFixture.Venues.Count);

        foreach (var a in B2BSeedFixture.Artists)
            await transport.PublishAsync(a, Envelope(typeof(Concertable.B2B.Artist.Contracts.Events.ArtistChangedEvent)), stoppingToken);
        logger.LogInformation("Published {Count} artist events", B2BSeedFixture.Artists.Count);

        var concerts = B2BSeedFixture.Concerts(now);
        foreach (var c in concerts)
            await transport.PublishAsync(c, Envelope(typeof(Concertable.B2B.Concert.Contracts.Events.ConcertChangedEvent)), stoppingToken);
        logger.LogInformation("Published {Count} concert events", concerts.Count);

        lifetime.StopApplication();
    }

    private static MessageEnvelope Envelope(Type eventType) =>
        new(Guid.NewGuid(), MessageTypeAttribute.Resolve(eventType), DateTimeOffset.UtcNow);
}
