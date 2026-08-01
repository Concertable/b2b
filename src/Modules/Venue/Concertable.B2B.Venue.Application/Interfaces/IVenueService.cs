using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Requests;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueService
{
    Task<Option<VenueDetails>> GetDetailsByIdAsync(int id);
    Task<Option<VenueDetails>> GetDetailsForCurrentUserAsync();
    Task<Result<VenueDetails, CreateVenueError>> CreateAsync(CreateVenueRequest request);
    Task<Result<VenueDetails, UpdateVenueError>> UpdateAsync(int id, UpdateVenueRequest request);
    Task<Option<int>> GetIdForCurrentUserAsync();
    Task<bool> OwnsVenueAsync(int venueId);
    Task<UnitResult<ApproveVenueError>> ApproveAsync(int id);

    Task<Option<VenueSummary>> GetSummaryAsync(int id);
    Task<Option<VenueOrgIdentity>> GetOrgIdentityByTenantIdAsync(Guid tenantId);
}
