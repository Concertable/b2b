using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Kernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertVenueHireApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ConcertVenueHireApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Finish_ShouldCompleteBookingAndFinishConcert()
    {
        // Arrange
        var concertId = fixture.SeedState.PastVenueHireBooking.Concert!.Id;

        // Act
        await fixture.FinishConcertAsync(concertId);

        // Assert
        var application = await fixture.ReadDbContext.Applications.FirstAsync(a => a.Id == fixture.SeedState.PastVenueHireApp.Id);
        Assert.Equal(LifecycleState.Complete, application.State);
        Assert.Empty(fixture.ManagerPaymentClient.Payments);
    }

    [Fact]
    public async Task Finish_ShouldFail_WhenConcertNotEnded()
    {
        // Arrange
        var concertId = fixture.SeedState.UpcomingVenueHireBooking.Concert!.Id;

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => fixture.FinishConcertAsync(concertId));
        var application = await fixture.ReadDbContext.Applications.FirstAsync(a => a.Id == fixture.SeedState.UpcomingVenueHireApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }
}
