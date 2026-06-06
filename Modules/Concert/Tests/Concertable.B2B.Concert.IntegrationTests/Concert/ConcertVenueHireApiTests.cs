using Concertable.B2B.Concert.Domain.Enums;
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
        var booking = await fixture.ReadDbContext.Bookings.FirstAsync(b => b.Id == fixture.SeedState.PastVenueHireBooking.Id);
        Assert.Equal(BookingStatus.Complete, booking.Status);
        var concert = await fixture.ReadDbContext.Concerts.FirstAsync(c => c.Id == concertId);
        Assert.Equal(ConcertStage.Finished, concert.CurrentStage);
        Assert.Empty(fixture.ManagerPaymentClient.Payments);
    }

    [Fact]
    public async Task Finish_ShouldFail_WhenConcertNotEnded()
    {
        // Arrange
        var concertId = fixture.SeedState.UpcomingVenueHireBooking.Concert!.Id;

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => fixture.FinishConcertAsync(concertId));
        var booking = await fixture.ReadDbContext.Bookings.FirstAsync(b => b.Id == fixture.SeedState.UpcomingVenueHireBooking.Id);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }
}
