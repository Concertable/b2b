using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task GetSubjectContractsAsync_TranslatesTheSetContainsToSql()
    {
        // Arrange — the point of this test is that IReadOnlySet<T>.Contains is a different expression-tree
        // method from ICollection<T>.Contains, and only the real provider decides whether it emits IN.
        var booking = await fixture.Bookings
            .SingleAsync(value => value.Id == fixture.SeedState.ConfirmedBooking.Id);
        var tenantIds = new HashSet<Guid> { booking.VenueTenantId };

        // Act
        var contracts = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IBookingModule>().GetSubjectContractsAsync(tenantIds));

        // Assert
        Assert.All(contracts, contract => Assert.False(string.IsNullOrWhiteSpace(contract.VenueName)));
    }

    [Fact]
    public async Task HasLiveObligations_TranslatesTheSetContainsToSql()
    {
        // Arrange
        var booking = await fixture.Bookings
            .SingleAsync(value => value.Id == fixture.SeedState.ConfirmedBooking.Id);
        var tenantIds = new HashSet<Guid> { booking.VenueTenantId };

        // Act
        var live = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IBookingModule>().HasLiveObligationsByTenantIdsAsync(tenantIds));

        // Assert — a Confirmed booking with no Concert acknowledgement is a live obligation (F1).
        Assert.True(live);
    }
}
