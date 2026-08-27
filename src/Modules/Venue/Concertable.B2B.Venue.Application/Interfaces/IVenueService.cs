using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Requests;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueService
{
    Task<Option<VenueDetails>> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default);
    Task<Option<VenueDetails>> GetDetailsAsync(
        CancellationToken ct = default);
    Task<Result<VenueDetails, CreateVenueError>> CreateAsync(
        CreateVenueRequest request,
        CancellationToken ct = default);
    Task<Result<VenueDetails, UpdateVenueError>> UpdateAsync(
        UpdateVenueRequest request,
        CancellationToken ct = default);
    Task<bool> OwnsVenueAsync(int venueId, CancellationToken ct = default);

    Task<Option<VenueSummary>> GetSummaryAsync(int id, CancellationToken ct = default);

    Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
