using Concertable.B2B.Artist.Application.DTOs;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistDashboardService
{
    Task<ArtistDashboardKpis?> GetKpisAsync(CancellationToken ct = default);
}
