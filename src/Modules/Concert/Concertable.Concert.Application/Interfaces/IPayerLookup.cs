using Concertable.Concert.Application.Responses;

namespace Concertable.Concert.Application.Interfaces;

internal interface IPayerLookup
{
    Task<Guid?> GetVenueManagerIdAsync(int applicationId);
    Task<Guid?> GetArtistManagerIdAsync(int applicationId);
    Task<(Guid VenueManagerId, Guid ArtistManagerId)?> GetManagerIdsAsync(int applicationId);
    Task<PayeeSummary?> GetArtistAsync(int applicationId);
    Task<PayeeSummary?> GetVenueAsync(int applicationId);
    Task<PayeeSummary?> GetVenueByOpportunityIdAsync(int opportunityId);
}
