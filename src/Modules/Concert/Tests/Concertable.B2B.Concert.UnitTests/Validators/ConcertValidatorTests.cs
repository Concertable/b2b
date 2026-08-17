using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Validators;
using Concertable.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests.Validators;

public sealed class ConcertValidatorTests
{
    private readonly ConcertValidator validator;

    public ConcertValidatorTests()
    {
        this.validator = new ConcertValidator();
    }

    [Fact]
    public void CanUpdate_TotalTicketsCoverTicketsSold_ReturnsSuccess()
    {
        var concert = CreateConcert();
        concert.IncrementTicketsSold(4);

        var result = this.validator.CanUpdate(concert, 4);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CanUpdate_TotalTicketsBelowTicketsSold_ReturnsStructuredFailure()
    {
        var concert = CreateConcert();
        concert.IncrementTicketsSold(4);

        var result = this.validator.CanUpdate(concert, 3);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            ["Cannot reduce total tickets below the 4 already sold."],
            errors.Errors["totalTickets"]);
    }

    [Fact]
    public void CanPost_UnconfirmedPostedConcert_AccumulatesOrderedStructuredErrors()
    {
        var concert = CreateConcert();
        concert.Post("Concert", "About", 10m, 100, DateTime.UtcNow);

        var result = this.validator.CanPost(concert, LifecycleState.Applied);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            ["Concert cannot be posted until the booking is confirmed"],
            errors.Errors["booking"]);
        Assert.Equal(["Concert has already been posted"], errors.Errors["datePosted"]);
        Assert.Equal(["booking", "datePosted"], errors.Errors.Keys);
    }

    [Fact]
    public void CanPost_ConfirmedUnpostedConcert_ReturnsValid()
    {
        var concert = CreateConcert();

        var result = this.validator.CanPost(concert, LifecycleState.Booked);

        Assert.True(result.IsValid);
    }

    private static ConcertEntity CreateConcert()
    {
        var application = StandardApplication.Create(
            1,
            2,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
        var booking = StandardBooking.Create(application.ToAccepted());
        var period = new DateRange(
            new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc));

        return ConcertEntity.CreateDraft(
            booking.ToConfirmed(2, period),
            "Concert",
            "About",
            [Genre.Rock]);
    }
}
