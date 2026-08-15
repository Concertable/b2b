using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReadRepository
{
    Task<VenueSummary?> GetSummaryAsync(int id);
    Task<VenueDetails?> GetDetailsByIdAsync(int id);
}
