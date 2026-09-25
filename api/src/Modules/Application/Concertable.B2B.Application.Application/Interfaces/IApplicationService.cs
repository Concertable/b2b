using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Application.Responses;
using Concertable.B2B.Application.Application.Errors;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationService
{
    Task<Result<ApplicationSummaryDto, ApplicationError>> GetSummaryAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<ApplicationProposalDto, ApplicationError>> GetProposalAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ApplicationProposalDto>, ApplicationError>> GetByOpportunityIdAsync(
        int id,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ApplicationProposalDto>, ApplicationError>> GetPendingForArtistAsync(
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ApplicationProposalDto>, ApplicationError>> GetRecentDeniedForArtistAsync(
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ApplicationSummaryDto>, ApplicationError>> GetPendingForCurrentVenueAsync(
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<ApplicationSummaryDto>, ApplicationError>> GetCurrentForCurrentArtistAsync(
        CancellationToken ct = default);
    Task<Result<ApplicationProposalDto, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        ESignatureRequest eSignature,
        CancellationToken ct = default);
    Task<bool> CanApplyAsync(int opportunityId);
    Task<bool> CanAcceptAsync(int applicationId);
    Task<Result<Checkout, ApplicationCheckoutError>> ApplyCheckoutAsync(int opportunityId);
    Task<Result<Checkout, ApplicationCheckoutError>> AcceptCheckoutAsync(int applicationId);
    Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        ESignatureRequest eSignature,
        CancellationToken ct = default);
    Task<UnitResult<WithdrawApplicationError>> WithdrawAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<UnitResult<RejectApplicationError>> RejectAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<UnitResult<CancelApplicationError>> CancelAsync(
        int applicationId,
        CancellationToken ct = default);
}
