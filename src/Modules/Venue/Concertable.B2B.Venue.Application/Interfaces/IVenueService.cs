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
    Task<UnitResult<ApproveVenueError>> ApproveAsync(
        int id,
        CancellationToken ct = default);

    Task<Option<VenueSummary>> GetSummaryAsync(int id, CancellationToken ct = default);
    Task<Option<int>> GetCurrentIdAsync(CancellationToken ct = default);
    Task<Option<VenueProfile>> GetProfileAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<VenueProfile>> GetProfilesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default);
    Task<Option<VenueProfile>> GetCurrentProfileAsync(CancellationToken ct = default);
    Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<IPagination<PendingVenue>> GetPendingApprovalAsync(IPageParams pageParams);

    Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
