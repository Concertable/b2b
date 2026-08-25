using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Application.Application.Errors;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationService
{
    Task<Result<ApplicationDto, ApplicationError>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetByOpportunityIdAsync(int id);
    Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetPendingForArtistAsync();
    Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetRecentDeniedForArtistAsync();
    Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetPendingForCurrentVenueAsync();
    Task<Result<IReadOnlyList<ApplicationDto>, ApplicationError>> GetCurrentForCurrentArtistAsync();
    Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(int opportunityId, ESignatureRequest eSignature);
    Task<Result<ApplicationDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        string paymentMethodId,
        ESignatureRequest eSignature);
    Task<bool> CanApplyAsync(int opportunityId);
    Task<bool> CanAcceptAsync(int applicationId);
    Task<Result<Checkout, ApplicationCheckoutError>> ApplyCheckoutAsync(int opportunityId);
    Task<Result<Checkout, ApplicationCheckoutError>> AcceptCheckoutAsync(int applicationId);
    Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default);
    Task<UnitResult<WithdrawApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<UnitResult<RejectApplicationError>> RejectAsync(
        int applicationId,
        CancellationToken ct = default);
}
