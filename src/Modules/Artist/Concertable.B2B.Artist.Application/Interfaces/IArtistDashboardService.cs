using Concertable.B2B.Artist.Application.DTOs;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistDashboardService
{
    Task<ArtistDashboardOverview?> GetOverviewAsync(CancellationToken ct = default);
    Task<ArtistDashboardKpis?> GetKpisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetPayoutsAsync(CancellationToken ct = default);
}
