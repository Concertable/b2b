using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Responses;
using Concertable.Concert.Domain.ReadModels;

namespace Concertable.Concert.Application.Interfaces;

internal interface IApplicationService
{
    Task<ApplicationDto> GetByIdAsync(int id);
    Task<IEnumerable<ApplicationDto>> GetByOpportunityIdAsync(int id);
    Task<IEnumerable<ApplicationDto>> GetPendingForArtistAsync();
    Task<IEnumerable<ApplicationDto>> GetRecentDeniedForArtistAsync();
    Task<ApplicationDto> ApplyAsync(int opportunityId);
    Task<ApplicationDto> ApplyAsync(int opportunityId, string paymentMethodId);
    Task<Checkout> ApplyCheckoutAsync(int opportunityId);
    Task<Checkout> AcceptCheckoutAsync(int applicationId);
    Task AcceptAsync(int applicationId, string? paymentMethodId);
    Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id);
}
