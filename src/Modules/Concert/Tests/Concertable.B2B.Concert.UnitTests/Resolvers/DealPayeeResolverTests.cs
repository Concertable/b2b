using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

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
        services.AddScoped(_ => Mock.Of<IConcertRepository>());
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
        var booking = (dealType switch
        {
            DealType.FlatFee => ConfirmedBookings.FlatFee(100m),
            DealType.DoorSplit => ConfirmedBookings.DoorSplit(50m),
            DealType.Versus => ConfirmedBookings.Versus(100m, 50m),
            DealType.VenueHire => ConfirmedBookings.VenueHire(100m),
            _ => throw new ArgumentOutOfRangeException(nameof(dealType), dealType, null)
        }) with { VenueTenantId = VenueTenantId, ArtistTenantId = ArtistTenantId };
        var concert = ConcertEntity.CreateDraft(booking, new ConcertDraft("Concert", "About", []));
        concert.Venue = new VenueReadModel { UserId = VenueUserId };
        concert.Artist = new ArtistReadModel { UserId = ArtistUserId };
        return concert;
    }
}
