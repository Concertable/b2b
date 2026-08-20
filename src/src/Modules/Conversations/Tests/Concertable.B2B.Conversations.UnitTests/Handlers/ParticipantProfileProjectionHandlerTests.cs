using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.Conversations.Infrastructure.Handlers;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.UnitTests.Handlers;

public sealed class ParticipantProfileProjectionHandlerTests
{
    [Fact]
    public async Task ArtistChanged_CreatesAndUpdatesParticipantProfile()
    {
        var databaseName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();

        await using (var context = NewContext(databaseName))
        {
            var handler = new ArtistParticipantProfileProjectionHandler(context);
            await handler.HandleAsync(ArtistEvent(tenantId, "Old name", "Old county", "Old town"), Envelope<ArtistChangedEvent>());
            await handler.HandleAsync(ArtistEvent(tenantId, "New name", "New county", "New town"), Envelope<ArtistChangedEvent>());
        }

        await using var read = NewContext(databaseName);
        var profile = await read.ParticipantProfiles.SingleAsync();
        Assert.Equal(tenantId, profile.TenantId);
        Assert.Equal("New name", profile.Name);
        Assert.Equal("New county", profile.Address.County);
        Assert.Equal("New town", profile.Address.Town);
    }

    [Fact]
    public async Task VenueChanged_CreatesParticipantProfileByTenantId()
    {
        var databaseName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var changed = new VenueChangedEvent(
            1, Guid.NewGuid(), "Venue", "About", "Avatar", "Banner", "County", "Town", 0, 0, "venue@example.com")
        {
            TenantId = tenantId
        };

        await using (var context = NewContext(databaseName))
            await new VenueParticipantProfileProjectionHandler(context).HandleAsync(changed, Envelope<VenueChangedEvent>());

        await using var read = NewContext(databaseName);
        var profile = await read.ParticipantProfiles.SingleAsync();
        Assert.Equal(tenantId, profile.TenantId);
        Assert.Equal("Venue", profile.Name);
    }

    [Fact]
    public async Task VenueChanged_WithoutTenantId_DoesNotCreateParticipantProfile()
    {
        await using var context = NewContext(Guid.NewGuid().ToString());
        var changed = new VenueChangedEvent(
            1, Guid.NewGuid(), "Venue", "About", "Avatar", "Banner", "County", "Town", 0, 0, "venue@example.com");

        await new VenueParticipantProfileProjectionHandler(context).HandleAsync(changed, Envelope<VenueChangedEvent>());

        Assert.Empty(await context.ParticipantProfiles.ToListAsync());
    }

    private static ArtistChangedEvent ArtistEvent(Guid tenantId, string name, string county, string town) =>
        new(1, Guid.NewGuid(), name, "About", "Avatar", "Banner", county, town, 0, 0, "artist@example.com", Array.Empty<Genre>(), tenantId);

    private static MessageEnvelope Envelope<TEvent>() where TEvent : IIntegrationEvent =>
        MessageEnvelope.Create<TEvent>(DateTimeOffset.UtcNow);

    private static ConversationsDbContext NewContext(string databaseName) =>
        new(
            new DbContextOptionsBuilder<ConversationsDbContext>().UseInMemoryDatabase(databaseName).Options,
            new ConversationsConfigurationProvider(),
            new StubTenantContext(Guid.NewGuid()));

    private sealed class StubTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid? TenantId { get; } = tenantId;
        public bool IsHost => false;
    }
}
