using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;

namespace Concertable.B2B.Application.Api.Resolvers;

internal interface IApplicationResponseResolver
{
    Task<ApplicationSummaryResponse> ResolveSummaryAsync(
        ApplicationSummary dto,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationSummaryResponse>> ResolveSummariesAsync(
        IReadOnlyList<ApplicationSummary> dtos,
        CancellationToken ct = default);
    Task<ApplicationProposalResponse> ResolveProposalAsync(
        ApplicationProposal dto,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationProposalResponse>> ResolveProposalsAsync(
        IReadOnlyList<ApplicationProposal> dtos,
        CancellationToken ct = default);
}
