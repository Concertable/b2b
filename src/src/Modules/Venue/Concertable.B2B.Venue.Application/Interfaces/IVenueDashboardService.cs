using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueDashboardService
{
    Task<Option<VenueDashboardKpis>> GetKpisAsync(CancellationToken ct = default);
}
