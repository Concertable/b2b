namespace Concertable.B2B.Venue.Application.DTOs;

public record VenueDashboardKpisDto(
    int ApplicationsToReview,
    int? ApplicationsToReviewDelta,
    int OpenOpportunities,
    int UpcomingConcerts,
    long MtdRevenueCents,
    double? MtdRevenueDeltaPercent);
