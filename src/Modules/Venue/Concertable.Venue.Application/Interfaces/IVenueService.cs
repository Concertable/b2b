using Concertable.Venue.Application.DTOs;
using Concertable.Venue.Application.Requests;

namespace Concertable.Venue.Application.Interfaces;

internal interface IVenueService
{
    Task<VenueDto> GetDetailsByIdAsync(int id);
    Task<VenueDto?> GetDetailsForCurrentUserAsync();
    Task<VenueDto> CreateAsync(CreateVenueRequest request);
    Task<VenueDto> UpdateAsync(int id, UpdateVenueRequest request);
    Task<int> GetIdForCurrentUserAsync();
    Task<bool> OwnsVenueAsync(int venueId);
    Task ApproveAsync(int id);
}
