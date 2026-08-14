using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueDashboardService
{
    Task<VenueDashboardOverview?> GetOverviewAsync(CancellationToken ct = default);
    Task<VenueDashboardKpis?> GetKpisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetTicketRevenueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Settlement>> GetSettlementsAsync(CancellationToken ct = default);
}
