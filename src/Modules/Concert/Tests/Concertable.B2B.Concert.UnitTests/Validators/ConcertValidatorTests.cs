using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Validators;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertValidatorTests
{
    private readonly ConcertValidator validator;

    public ConcertValidatorTests()
    {
        validator = new ConcertValidator();
    }

    [Fact]
    public void CanUpdate_TotalTicketsCoverTicketsSold_ReturnsSuccess()
    {
        var concert = CreateConcert();
        concert.IncrementTicketsSold(4);

        var result = validator.CanUpdate(concert, 4);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CanUpdate_TotalTicketsBelowTicketsSold_ReturnsStructuredFailure()
    {
        var concert = CreateConcert();
        concert.IncrementTicketsSold(4);

        var result = validator.CanUpdate(concert, 3);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            ["Cannot reduce total tickets below the 4 already sold."],
            errors.Errors["totalTickets"]);
    }

    [Fact]
    public void CanPost_PostedConcert_ReturnsStructuredFailure()
    {
        var concert = CreateConcert();
        concert.Post("Concert", "About", 10m, 100, DateTime.UtcNow);

        var result = validator.CanPost(concert);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(["Concert has already been posted"], errors.Errors["datePosted"]);
    }

    [Fact]
    public void CanPost_UnpostedConcert_ReturnsValid()
    {
        var concert = CreateConcert();

        var result = validator.CanPost(concert);

        Assert.True(result.IsValid);
    }

    private static ConcertEntity CreateConcert()
    {
        var booking = new ConfirmedBooking(
            Guid.NewGuid(),
            1,
            2,
            3,
            4,
            5,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DealType.FlatFee,
            false,
            new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc),
            [Genre.Rock],
            new FlatFeeTerms(100m));

        return ConcertEntity.CreateDraft(
            booking,
            "Concert",
            "About",
            [Genre.Rock]);
    }
}
