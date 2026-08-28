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
    public async Task Finish_WhenPersistenceFailsAfterRelease_RetryUsesTheSameOperation()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking);
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concert.Id);
        await fixture.FailSettlementPersistenceAsync();

        try
        {
            await Assert.ThrowsAnyAsync<DbUpdateException>(
                () => fixture.CompleteConcertAsync(concert.Id));
        }
        finally
        {
            await fixture.RestoreSettlementPersistenceAsync();
        }

        var interrupted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(State.AwaitingSettlement, interrupted.State);
        Assert.NotNull(interrupted.SettlementOperationId);

        var retry = await fixture.CompleteConcertAsync(concert.Id);

        Assert.True(retry.TryGetValue(out var outcome));
        Assert.Equal(SettlementOutcome.Settled, outcome);
        var release = Assert.Single(
            fixture.EscrowClient.Releases,
            value => value.BookingId == concert.BookingId);
        Assert.Equal(interrupted.SettlementOperationId, release.OperationId);
        var settled = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(State.Complete, settled.State);
        Assert.NotNull(await fixture.Invoices.SingleOrDefaultAsync(invoice => invoice.BookingId == concert.BookingId));
    }

    [Fact]
    public async Task Finish_WhenAnotherFinishWinsTheRace_ReleasesEscrowAndIssuesInvoiceOnce()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking);
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concert.Id);
        fixture.ArmConcertConflict(async () =>
        {
            var winner = await fixture.CompleteConcertAsync(concert.Id);
            Assert.True(winner.TryGetValue(out _));
        });

        var loser = await fixture.CompleteConcertAsync(concert.Id);

        Assert.True(loser.TryGetValue(out var outcome));
        Assert.Equal(SettlementOutcome.Settled, outcome);
        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        var release = Assert.Single(
            fixture.EscrowClient.Releases,
            value => value.BookingId == concert.BookingId);
        var settled = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(release.OperationId, settled.SettlementOperationId);
        Assert.Equal(State.Complete, settled.State);
        Assert.Equal(1, await fixture.Invoices.CountAsync(invoice => invoice.BookingId == concert.BookingId));
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
