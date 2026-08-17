using System.Net;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests.Domain;

public sealed class TenantInheritanceTests
{
    private readonly Guid venueTenantId = Guid.NewGuid();
    private readonly Guid artistTenantId = Guid.NewGuid();

    [Fact]
    public void Create_PropagatesTenantPairThroughDomainFactories()
    {
        var application = StandardApplication.Create(
            1,
            2,
            DealType.FlatFee,
            venueTenantId,
            artistTenantId);
        var booking = StandardBooking.Create(application.ToAccepted());
        var period = new DateRange(
            new DateTime(2026, 8, 8, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 8, 22, 0, 0, DateTimeKind.Utc));
        var concert = ConcertEntity.CreateDraft(booking.ToConfirmed(2, period), "Concert", "About", []);
        var signature = new ESignature(
            Guid.NewGuid(),
            DateTime.UtcNow,
            IPAddress.Loopback,
            "tests",
            "Signatory",
            null);
        var contract = ContractEntity.Create(
            booking,
            2,
            "Venue",
            1,
            "Artist",
            period,
            new FlatFeeDeal { PaymentMethod = PaymentMethod.Transfer, Fee = 100m },
            "Terms",
            "2026-08",
            signature,
            signature,
            DateTime.UtcNow);
        var party = new InvoiceParty(Guid.NewGuid(), "Party", null, "Line 1", null, "City", "AB1 2CD", "GB");
        var invoice = InvoiceEntity.Create(
            concert,
            party,
            party,
            new VatBreakdown(100m, 20m, 120m, 0.2m),
            1,
            "INV-000001",
            period.End,
            DateTime.UtcNow);

        AssertScope(application, venueTenantId, artistTenantId);
        AssertScope(booking, venueTenantId, artistTenantId);
        AssertScope(concert, venueTenantId, artistTenantId);
        AssertScope(contract, venueTenantId, artistTenantId);
        AssertScope(invoice, venueTenantId, artistTenantId);
        Assert.Equal(application.Id, booking.ApplicationId);
        Assert.Equal(booking.Id, concert.BookingId);
        Assert.Same(booking, contract.Booking);
        Assert.Equal(booking.Id, invoice.BookingId);
        Assert.Equal(DealType.FlatFee, application.DealType);
        Assert.Equal(DealType.FlatFee, contract.DealType);
        Assert.Equal(DealType.FlatFee, invoice.DealType);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CreateApplication_Throws_WhenEitherTenantIsUnresolved(bool emptyVenue, bool emptyArtist)
    {
        var venue = emptyVenue ? Guid.Empty : venueTenantId;
        var artist = emptyArtist ? Guid.Empty : artistTenantId;

        Assert.Throws<InvalidOperationException>(
            () => StandardApplication.Create(1, 2, DealType.FlatFee, venue, artist));
    }

    private static void AssertScope(
        Concertable.B2B.DataAccess.Application.IVenueArtistTenantScoped entity,
        Guid expectedVenueTenantId,
        Guid expectedArtistTenantId)
    {
        Assert.Equal(expectedVenueTenantId, entity.VenueTenantId);
        Assert.Equal(expectedArtistTenantId, entity.ArtistTenantId);
    }
}
