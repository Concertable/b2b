using Concertable.Artist.Application.DTOs;

namespace Concertable.Artist.Application.Interfaces;

internal interface IArtistDashboardService
{
    Task<ArtistDashboardKpisDto?> GetKpisAsync(CancellationToken ct = default);
}
