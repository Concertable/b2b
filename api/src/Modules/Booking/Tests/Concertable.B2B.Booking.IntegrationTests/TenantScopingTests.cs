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
        var booking = await this.fixture.Bookings
            .SingleAsync(value => value.Id == this.fixture.SeedState.ConfirmedBooking.Id);
        var tenantIds = new HashSet<Guid> { booking.VenueTenantId };

        var contracts = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IBookingModule>().GetSubjectContractsAsync(tenantIds));

        Assert.All(contracts, contract => Assert.NotEqual(default, contract.CreatedAtUtc));
    }

    [Fact]
    public async Task HasLiveObligations_TranslatesTheSetContainsToSql()
    {
        var booking = await this.fixture.Bookings
            .SingleAsync(value => value.Id == this.fixture.SeedState.ConfirmedBooking.Id);
        var tenantIds = new HashSet<Guid> { booking.VenueTenantId };

        var live = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IBookingModule>().HasLiveObligationsByTenantIdsAsync(tenantIds));

        Assert.True(live);
    }
}
