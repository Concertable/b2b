namespace Concertable.B2B.Venue.Application.DTOs;

public sealed record VenueDashboardKpis(
    int ApplicationsToReview,
    int? ApplicationsToReviewDelta,
    int OpenOpportunities,
    int UpcomingConcerts,
    int AwaitingDoorRevenue,
    long MtdRevenueCents,
    double? MtdRevenueDeltaPercent);
