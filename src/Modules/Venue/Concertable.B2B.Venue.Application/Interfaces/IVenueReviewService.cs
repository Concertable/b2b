using Concertable.Contracts;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReviewService
{
    Task<ReviewSummaryDto> GetSummaryAsync(int venueId);
}
