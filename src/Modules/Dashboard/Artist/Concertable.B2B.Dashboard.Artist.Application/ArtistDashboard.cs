using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Dashboard.Artist.Application;

internal sealed record ArtistDashboardKpis(
    int PendingApplications,
    int AcceptedAwaitingCheckout,
    int UpcomingConcerts,
    long MtdPayoutsCents,
    double? MtdPayoutsDeltaPercent);

internal sealed record ArtistDashboardOverview(
    int ArtistId,
    string ArtistName,
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

internal sealed record MonthlyRevenuePoint(
    DateOnly Month,
    long GrossCents,
    long NetCents,
    int Count);

internal interface IArtistDashboardService
{
    Task<Option<ArtistDashboardKpis>> GetAsync(CancellationToken ct = default);
    Task<Option<ArtistDashboardOverview>> GetOverviewAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetPayoutsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(CancellationToken ct = default);
}
