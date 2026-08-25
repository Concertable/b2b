using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Lifecycle;
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
        var concert = await fixture.Concerts.SingleAsync(value => value.Id == concertId);
        Assert.Equal(State.Complete, concert.State);
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
        var concert = await fixture.Concerts.SingleAsync(value => value.Id == concertId);
        Assert.Equal(State.Draft, concert.State);
    }

    [Fact]
    public async Task Finish_ShouldBeIdempotent_WhenAlreadyFinished()
    {
        // Arrange
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking).Id;
        await fixture.FinishConcertAsync(concertId);

        // Act & Assert
        var result = await fixture.FinishConcertAsync(concertId);

        Assert.True(result.TryGetValue(out var outcome));
        Assert.Equal(SettlementOutcome.Settled, outcome);
        var concert = await fixture.Concerts.SingleAsync(value => value.Id == concertId);
        Assert.Equal(State.Complete, concert.State);
    }
}
