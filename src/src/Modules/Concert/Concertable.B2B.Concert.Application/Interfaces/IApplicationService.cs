using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationService
{
    Task<Option<ApplicationDto>> GetByIdAsync(int id);
    Task<IReadOnlyList<ApplicationDto>> GetByOpportunityIdAsync(int id);
    Task<IReadOnlyList<ApplicationDto>> GetPendingForArtistAsync();
    Task<IReadOnlyList<ApplicationDto>> GetRecentDeniedForArtistAsync();
    Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(int opportunityId, ESignatureRequest eSignature);
    Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string paymentMethodId,
        ESignatureRequest eSignature);
    Task<Checkout> ApplyCheckoutAsync(int opportunityId);
    Task<Checkout> AcceptCheckoutAsync(int applicationId);
    Task AcceptAsync(int applicationId, string? paymentMethodId, ESignatureRequest eSignature);
    Task WithdrawAsync(int applicationId);
    Task<UnitResult<RejectApplicationError>> RejectAsync(int applicationId);
    Task CancelAsync(int applicationId);
    Task<Option<(ArtistReadModel, VenueReadModel)>> GetArtistAndVenueByIdAsync(int id);
}
