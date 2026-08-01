using Concertable.B2B.Concert.Domain.Entities;
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

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CanUpdate_TotalTicketsBelowTicketsSold_ReturnsStructuredFailure()
    {
        var concert = CreateConcert();
        concert.IncrementTicketsSold(4);

        var result = this.validator.CanUpdate(concert, 3);

        Assert.True(result.TryGetError(out var errors));
        Assert.Equal(
            ["Cannot reduce total tickets below the 4 already sold."],
            errors.Errors["totalTickets"]);
    }

    private static ConcertEntity CreateConcert() =>
        ConcertEntity.CreateDraft(
            1,
            2,
            3,
            new DateRange(
                new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc)),
            "Concert",
            "About",
            DealType.FlatFee,
            [Genre.Rock]);
}
