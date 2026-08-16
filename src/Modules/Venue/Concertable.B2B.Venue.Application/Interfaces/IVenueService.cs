using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Requests;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueService
{
    Task<Result<VenueDetails, VenueError>> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<VenueDetails, VenueError>> GetDetailsForActiveTenantAsync(
        CancellationToken ct = default);
    Task<Result<VenueDetails, CreateVenueError>> CreateForActiveTenantAsync(
        CreateVenueRequest request,
        CancellationToken ct = default);
    Task<Result<VenueDetails, UpdateVenueError>> UpdateForActiveTenantAsync(
        UpdateVenueRequest request,
        CancellationToken ct = default);
    Task<bool> OwnsVenueAsync(int venueId, CancellationToken ct = default);
    Task<UnitResult<ApproveVenueError>> ApproveAsync(
        int id,
        CancellationToken ct = default);

    Task<Option<VenueSummary>> GetSummaryAsync(int id, CancellationToken ct = default);
}
