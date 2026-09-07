using Concertable.Contracts.Enums;

namespace Concertable.B2B.Dashboard.Opportunity.Application;

internal static class OpportunityDashboardCalculator
{
    public static int CalculateFitScore(
        IReadOnlySet<Genre> opportunityGenres,
        IReadOnlySet<Genre> artistGenres)
    {
        if (opportunityGenres.Count == 0)
            return 100;

        var matchingGenres = opportunityGenres.Count(artistGenres.Contains);
        return (int)Math.Round(matchingGenres * 100d / opportunityGenres.Count);
    }

    public static int CalculateDaysUntilDeadline(DateTime startDate, DateTime today) =>
        Math.Max(0, (startDate.Date.AddDays(-7) - today.Date).Days);
}
