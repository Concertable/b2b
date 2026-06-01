namespace Concertable.B2B.Venue.Application.DTOs;

public sealed record VenueDashboardKpisDto(
    int ApplicationsToReview,
    int? ApplicationsToReviewDelta,
    int OpenOpportunities,
    int UpcomingConcerts,
    long MtdRevenueCents,
    double? MtdRevenueDeltaPercent);
