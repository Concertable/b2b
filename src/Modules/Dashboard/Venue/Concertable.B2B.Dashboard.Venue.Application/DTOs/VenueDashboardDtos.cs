using Concertable.Contracts;

namespace Concertable.B2B.Dashboard.Venue.Application;

internal sealed record VenueDashboardKpis(
    int ApplicationsToReview,
    int? ApplicationsToReviewDelta,
    int OpenOpportunities,
    int UpcomingConcerts,
    int AwaitingDoorRevenue,
    long MtdRevenueCents,
    double? MtdRevenueDeltaPercent);

internal sealed record VenueDashboardOverview(
    int VenueId,
    string VenueName,
    ProfileHealth ProfileHealth,
    StripeConnectStatus StripeConnect,
    ReviewSummary ReviewSummary);

internal sealed record ProfileHealth(int Completeness, IReadOnlyList<ProfileHealthItem> Items);

internal sealed record ProfileHealthItem(string Id, string Label, string Href, bool Done);

internal sealed record StripeConnectStatus(StripeConnectState State, string Href);

internal enum StripeConnectState
{
    Complete,
    Incomplete,
    ActionRequired,
    Pending
}

internal sealed record MonthlyRevenuePoint(DateOnly Month, long GrossCents, long NetCents, int Count);

internal sealed record Settlement(
    int Id,
    int ConcertId,
    string ConcertName,
    DateTime At,
    long AmountCents,
    string CounterpartyName,
    SettlementDirection Direction);

internal enum SettlementDirection
{
    In,
    Out
}
