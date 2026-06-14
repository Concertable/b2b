using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Kernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertFlatFeeApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ConcertFlatFeeApiTests(ApiFixture fixture, ITestOutputHelper output)
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
        var concertId = fixture.SeedState.PastFlatFeeBooking.Concert!.Id;

        // Act
        await fixture.FinishConcertAsync(concertId);

        // Assert
        var application = await fixture.Applications.FirstAsync(a => a.Id == fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(LifecycleState.Complete, application.State);
        Assert.Empty(fixture.ManagerPaymentClient.Payments);
    }

    [Fact]
    public async Task Finish_ShouldFail_WhenConcertNotEnded()
    {
        // Arrange
        var concertId = fixture.SeedState.UpcomingFlatFeeBooking.Concert!.Id;

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => fixture.FinishConcertAsync(concertId));
        var application = await fixture.Applications.FirstAsync(a => a.Id == fixture.SeedState.UpcomingFlatFeeApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }

    [Fact]
    public async Task Finish_ShouldFail_WhenAlreadyFinished()
    {
        // Arrange
        var concertId = fixture.SeedState.PastFlatFeeBooking.Concert!.Id;
        await fixture.FinishConcertAsync(concertId);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => fixture.FinishConcertAsync(concertId));
    }
}
