using Concertable.Venue.Application.DTOs;

namespace Concertable.Venue.Application.Interfaces;

internal interface IVenueDashboardService
{
    Task<VenueDashboardKpisDto?> GetKpisAsync(CancellationToken ct = default);
}
