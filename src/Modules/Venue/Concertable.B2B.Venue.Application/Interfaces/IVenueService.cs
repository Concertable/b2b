using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Requests;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueService
{
    Task<VenueDetails> GetDetailsByIdAsync(int id);
    Task<VenueDetails?> GetDetailsForCurrentUserAsync();
    Task<VenueDetails> CreateAsync(CreateVenueRequest request);
    Task<VenueDetails> UpdateAsync(int id, UpdateVenueRequest request);
    Task<int> GetIdForCurrentUserAsync();
    Task<bool> OwnsVenueAsync(int venueId);
    Task ApproveAsync(int id);

    Task<VenueSummary> GetSummaryAsync(int id);
}
