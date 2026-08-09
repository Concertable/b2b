using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.UnitTests.Resolvers;

public sealed class DealPayeeResolverTests
{
    private static readonly Guid VenueUserId = Guid.NewGuid();
    private static readonly Guid VenueTenantId = Guid.NewGuid();
    private static readonly Guid ArtistUserId = Guid.NewGuid();
    private static readonly Guid ArtistTenantId = Guid.NewGuid();

    [Theory]
    [InlineData(DealType.FlatFee, true)]
    [InlineData(DealType.DoorSplit, true)]
    [InlineData(DealType.Versus, true)]
    [InlineData(DealType.VenueHire, false)]
    public void Resolve_DealType_ReturnsExpectedTicketAndSettlementRecipients(
        DealType dealType,
        bool venueCollectsTickets)
    {
        var services = new ServiceCollection();
        services.AddConcertDealStrategies();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IDealPayeeResolver>();
        var concert = CreateConcert(dealType);
        var expectedTicketUserId = venueCollectsTickets ? VenueUserId : ArtistUserId;
        var expectedTicketTenantId = venueCollectsTickets ? VenueTenantId : ArtistTenantId;
        var expectedSettlementTenantId = venueCollectsTickets ? ArtistTenantId : VenueTenantId;

        var ticketUserId = resolver.ResolveTicketUserId(concert);
        var ticketTenantId = resolver.ResolveTicketTenantId(concert);
        var settlementTenantId = resolver.ResolveSettlementTenantId(concert);

        Assert.Equal(expectedTicketUserId, ticketUserId);
        Assert.Equal(expectedTicketTenantId, ticketTenantId);
        Assert.Equal(expectedSettlementTenantId, settlementTenantId);
    }

    private static ConcertEntity CreateConcert(DealType dealType)
    {
        var application = StandardApplication.Create(
            1,
            2,
            dealType,
            VenueTenantId,
            ArtistTenantId);
        var booking = StandardBooking.Create(application);
        var period = new DateRange(
            new DateTime(2026, 8, 9, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 9, 22, 0, 0, DateTimeKind.Utc));
        var concert = ConcertEntity.CreateDraft(booking, 1, 2, period, "Concert", "About", []);
        concert.Venue = new VenueReadModel { UserId = VenueUserId };
        concert.Artist = new ArtistReadModel { UserId = ArtistUserId };
        return concert;
    }
}
