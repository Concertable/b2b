using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertFlatFeeApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertFlatFeeApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
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
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking).Id;

        // Act
        await fixture.FinishConcertAsync(concertId);

        // Assert
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(LifecycleState.Complete, application.State);
        Assert.Empty(fixture.ManagerPaymentClient.Payments);
    }

    [Fact]
    public async Task Finish_ShouldFail_WhenConcertNotEnded()
    {
        // Arrange
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.UpcomingFlatFeeBooking).Id;

        // Act & Assert
        var result = await fixture.FinishConcertAsync(concertId);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<FinishConcertError.ConcertNotEnded>(error);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == fixture.SeedState.UpcomingFlatFeeApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }

    [Fact]
    public async Task Finish_ShouldFail_WhenAlreadyFinished()
    {
        // Arrange
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking).Id;
        await fixture.FinishConcertAsync(concertId);

        // Act & Assert
        var result = await fixture.FinishConcertAsync(concertId);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<FinishConcertError.TransitionFailure>(error);
    }
}
