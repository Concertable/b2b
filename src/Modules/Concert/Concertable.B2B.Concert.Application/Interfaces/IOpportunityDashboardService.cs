using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Reunion;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IOpportunityDashboardService
{
    Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityError>>
        GetApplicationMetricsForCurrentVenueAsync();

    Task<Result<IReadOnlyList<OpportunityMatch>, OpportunityError>> GetMatchesForCurrentArtistAsync();
}
