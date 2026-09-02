using Concertable.B2B.Tenant.Contracts;
using Reunion;

namespace Concertable.B2B.Dashboard.Venue.Application;

internal interface IVenueDashboardService
{
    Task<Option<VenueDashboardKpis>> GetAsync(CancellationToken ct = default);
    Task<Option<VenueDashboardOverview>> GetOverviewAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetTicketRevenueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Settlement>> GetSettlementsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(CancellationToken ct = default);
}
