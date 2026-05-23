using Concertable.Contracts;

namespace Concertable.Venue.Application.Interfaces;

internal interface IVenueReviewService
{
    Task<ReviewSummaryDto> GetSummaryAsync(int venueId);
}
