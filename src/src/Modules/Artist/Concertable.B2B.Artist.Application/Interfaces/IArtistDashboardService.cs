using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistDashboardService
{
    Task<Result<ArtistDashboardOverview, ArtistError>> GetOverviewAsync(CancellationToken ct = default);
    Task<Option<ArtistDashboardKpis>> GetKpisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyRevenuePoint>> GetPayoutsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(CancellationToken ct = default);
}
