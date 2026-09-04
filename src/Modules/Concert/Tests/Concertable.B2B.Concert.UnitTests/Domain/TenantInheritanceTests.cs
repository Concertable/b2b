using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class TenantInheritanceTests
{
    private readonly Guid venueTenantId = Guid.NewGuid();
    private readonly Guid artistTenantId = Guid.NewGuid();

    [Fact]
    public void Create_ConfirmedBooking_PropagatesTenantPairToConcertAndInvoice()
    {
        var booking = CreateBooking(venueTenantId, artistTenantId);
        var concert = ConcertEntity.CreateDraft(booking, "Concert", "About", []);
        var party = new InvoiceParty(Guid.NewGuid(), "Party", null, "Line 1", null, "City", "AB1 2CD", "GB");
        var invoice = InvoiceEntity.Create(
            concert,
            party,
            party,
            new VatBreakdown(100m, 20m, 120m, 0.2m),
            1,
            "INV-000001",
            booking.EndDate,
            DateTime.UtcNow);

        AssertScope(concert, venueTenantId, artistTenantId);
        AssertScope(invoice, venueTenantId, artistTenantId);
        Assert.Equal(booking.BookingId, concert.BookingId);
        Assert.Equal(booking.BookingId, invoice.BookingId);
        Assert.Equal(DealType.FlatFee, invoice.DealType);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CreateDraft_UnresolvedBookingTenant_ThrowsInvalidOperationException(bool emptyVenue, bool emptyArtist)
    {
        var venue = emptyVenue ? Guid.Empty : venueTenantId;
        var artist = emptyArtist ? Guid.Empty : artistTenantId;
        var booking = CreateBooking(venue, artist);

        Assert.Throws<InvalidOperationException>(
            () => ConcertEntity.CreateDraft(booking, "Concert", "About", []));
    }

    private static ConfirmedBookingSnapshot CreateBooking(Guid venueTenantId, Guid artistTenantId) =>
        new(
            Guid.NewGuid(),
            1,
            2,
            3,
            4,
            5,
            venueTenantId,
            artistTenantId,
            DealType.FlatFee,
            false,
            new DateTime(2026, 8, 8, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 8, 22, 0, 0, DateTimeKind.Utc),
            [],
            new FlatFeeTerms(100m));

    private static void AssertScope(
        Concertable.B2B.DataAccess.Application.IVenueArtistTenantScoped entity,
        Guid expectedVenueTenantId,
        Guid expectedArtistTenantId)
    {
        Assert.Equal(expectedVenueTenantId, entity.VenueTenantId);
        Assert.Equal(expectedArtistTenantId, entity.ArtistTenantId);
    }
}
