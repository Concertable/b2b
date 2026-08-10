using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationService
{
    Task<Result<ApplicationDto, ApplicationError>> GetByIdAsync(int id);
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
    Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default);
    Task<UnitResult<CancelApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<UnitResult<RejectApplicationError>> RejectAsync(int applicationId);
    Task<UnitResult<CancelApplicationError>> CancelAsync(int applicationId, CancellationToken ct = default);
    Task<Option<(ArtistReadModel, VenueReadModel)>> GetArtistAndVenueByIdAsync(int id);
}
