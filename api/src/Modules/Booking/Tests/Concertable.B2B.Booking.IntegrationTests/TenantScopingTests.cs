using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Booking.IntegrationTests;

[Collection("Integration")]
public sealed class TenantScopingTests : IAsyncLifetime
{
    private readonly BookingApiFixture fixture;

    public TenantScopingTests(BookingApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task BookingReadStance_ResolvesBookingsWithoutTenantContext()
    {
        var booking = await fixture.Bookings
            .SingleAsync(value => value.Id == fixture.SeedState.ConfirmedBooking.Id);

        Assert.Equal(fixture.SeedState.ConfirmedApp.Id, booking.ApplicationId);
    }
}
