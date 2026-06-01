namespace Concertable.B2B.Concert.Contracts;

public sealed record VenueDashboardCountsDto(
    int ApplicationsToReview,
    int OpenOpportunities,
    int UpcomingConcerts);
