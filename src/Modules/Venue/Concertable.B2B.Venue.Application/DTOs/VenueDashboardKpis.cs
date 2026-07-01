namespace Concertable.B2B.Venue.Application.DTOs;

public sealed record VenueDashboardKpis(
    int ApplicationsToReview,
    int? ApplicationsToReviewDelta,
    int OpenOpportunities,
    int UpcomingConcerts,
    long MtdRevenueCents,
    double? MtdRevenueDeltaPercent);
