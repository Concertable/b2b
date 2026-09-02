using Concertable.B2B.Tenant.Contracts;
using Reunion;

namespace Concertable.B2B.Dashboard.Artist.Application;

internal interface IArtistDashboardService
{
    Task<Option<ArtistDashboardKpis>> GetAsync(CancellationToken ct = default);
    Task<Option<ArtistDashboardOverview>> GetOverviewAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetPayoutsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(CancellationToken ct = default);
}
