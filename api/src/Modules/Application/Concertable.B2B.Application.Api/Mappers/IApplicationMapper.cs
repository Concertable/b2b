using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;

namespace Concertable.B2B.Application.Api.Mappers;

internal interface IApplicationMapper
{
    Task<ApplicationSummaryResponse> ToSummaryResponseAsync(
        ApplicationSummaryDto dto,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationSummaryResponse>> ToSummaryResponsesAsync(
        IReadOnlyList<ApplicationSummaryDto> dtos,
        CancellationToken ct = default);
    Task<ApplicationProposalResponse> ToProposalResponseAsync(
        ApplicationProposalDto dto,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationProposalResponse>> ToProposalResponsesAsync(
        IReadOnlyList<ApplicationProposalDto> dtos,
        CancellationToken ct = default);
}
