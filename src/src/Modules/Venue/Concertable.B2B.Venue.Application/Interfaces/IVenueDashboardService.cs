using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueDashboardService
{
    Task<Option<VenueDashboardOverview>> GetOverviewAsync(CancellationToken ct = default);
    Task<Option<VenueDashboardKpis>> GetKpisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetTicketRevenueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Settlement>> GetSettlementsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(CancellationToken ct = default);
}
