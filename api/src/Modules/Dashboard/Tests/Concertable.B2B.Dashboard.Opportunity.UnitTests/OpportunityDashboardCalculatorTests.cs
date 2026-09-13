using Concertable.B2B.Dashboard.Opportunity.Application;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Dashboard.Opportunity.UnitTests;

public sealed class OpportunityDashboardCalculatorTests
{
    [Fact]
    public void CalculateFitScore_NoRequiredGenres_ReturnsPerfectFit()
    {
        var score = OpportunityDashboardCalculator.CalculateFitScore(
            new HashSet<Genre>(),
            new HashSet<Genre> { Genre.Rock });

        Assert.Equal(100, score);
    }

    [Fact]
    public void CalculateFitScore_PartialOverlap_ReturnsRoundedPercentage()
    {
        var score = OpportunityDashboardCalculator.CalculateFitScore(
            new HashSet<Genre> { Genre.Rock, Genre.Pop, Genre.Jazz },
            new HashSet<Genre> { Genre.Rock, Genre.Jazz });

        Assert.Equal(67, score);
    }

    [Theory]
    [InlineData("2026-09-20", "2026-09-01", 12)]
    [InlineData("2026-09-05", "2026-09-01", 0)]
    public void CalculateDaysUntilDeadline_ReturnsNonNegativeDays(
        DateTime startDate,
        DateTime today,
        int expected)
    {
        var days = OpportunityDashboardCalculator.CalculateDaysUntilDeadline(startDate, today);

        Assert.Equal(expected, days);
    }
}
