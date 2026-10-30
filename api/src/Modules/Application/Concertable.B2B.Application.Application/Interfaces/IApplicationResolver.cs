using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationResolver
{
    Task<ApplicationSummary> ResolveSummaryAsync(ApplicationEntity application, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationSummary>> ResolveSummariesAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default);
    Task<ApplicationProposal> ResolveProposalAsync(ApplicationEntity application, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationProposal>> ResolveProposalsAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default);
}
