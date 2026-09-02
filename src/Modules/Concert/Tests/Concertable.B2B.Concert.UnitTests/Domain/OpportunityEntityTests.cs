using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;
using Xunit;

namespace Concertable.B2B.Concert.UnitTests.Domain;

public sealed class OpportunityEntityTests
{
    private static readonly DateRange Period = new(
        new DateTime(2035, 1, 1, 19, 0, 0, DateTimeKind.Utc),
        new DateTime(2035, 1, 1, 22, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Create_DuplicateGenre_IsStoredOnceInInsertionOrder()
    {
        var opportunity = OpportunityEntity.Create(1, Period, 2, [Genre.Rock, Genre.Rock, Genre.Jazz]);

        Assert.Equal([Genre.Rock, Genre.Jazz], opportunity.Genres);
    }

    [Fact]
    public void Update_DuplicateGenre_IsStoredOnce()
    {
        var opportunity = OpportunityEntity.Create(1, Period, 2);

        opportunity.Update(Period, 2, [Genre.House, Genre.House]);

        Assert.Equal([Genre.House], opportunity.Genres);
    }
}
