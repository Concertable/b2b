using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;

namespace Concertable.B2B.Application.Api.Mappers;

internal interface IApplicationMapper
{
    Task<ApplicationSummaryResponse> ToSummaryResponseAsync(ApplicationSummaryDto dto);
    Task<IReadOnlyList<ApplicationSummaryResponse>> ToSummaryResponsesAsync(
        IReadOnlyList<ApplicationSummaryDto> dtos);
    Task<ApplicationProposalResponse> ToProposalResponseAsync(ApplicationProposalDto dto);
    Task<IReadOnlyList<ApplicationProposalResponse>> ToProposalResponsesAsync(
        IReadOnlyList<ApplicationProposalDto> dtos);
}
