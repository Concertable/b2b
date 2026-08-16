using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.Kernel.ValueObjects;
using Xunit;

namespace Concertable.B2B.Concert.UnitTests.Domain;

public sealed class BookingEntityTests
{
    [Fact]
    public void Confirm_LinksConcert_AndRaisesBookingConfirmedDomainEvent_CarryingTenantsNamesAndPeriod()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var application = StandardApplication.Create(1, 2, DealType.FlatFee, venueTenantId, artistTenantId);
        var booking = StandardBooking.Create(application);
        var period = new DateRange(
            new DateTime(2035, 1, 1, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2035, 1, 1, 22, 0, 0, DateTimeKind.Utc));
        var concert = ConcertEntity.CreateDraft(booking, 1, 2, period, "Concert", "About", []);

        booking.Confirm(concert, "The Venue", "The Artist");

        Assert.Same(concert, booking.Concert);
        var raised = Assert.IsType<BookingConfirmedDomainEvent>(Assert.Single(booking.DomainEvents));
        Assert.Equal(venueTenantId, raised.VenueTenantId);
        Assert.Equal("The Venue", raised.VenueName);
        Assert.Equal(artistTenantId, raised.ArtistTenantId);
        Assert.Equal("The Artist", raised.ArtistName);
        Assert.Equal(period, raised.Period);
    }
}
