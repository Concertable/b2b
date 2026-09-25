using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Mappers;

internal interface IApplicationMapper
{
    Task<ApplicationSummaryDto> ToSummaryAsync(ApplicationEntity application, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationSummaryDto>> ToSummariesAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default);
    Task<ApplicationProposalDto> ToProposalAsync(ApplicationEntity application, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationProposalDto>> ToProposalsAsync(
        IEnumerable<ApplicationEntity> applications,
        CancellationToken ct = default);
}
