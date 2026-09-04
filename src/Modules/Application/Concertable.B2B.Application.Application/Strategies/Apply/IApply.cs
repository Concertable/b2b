using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface IApply : IDealStrategy
{
    Task<Result<ApplicationEntity, ApplyApplicationError>> ApplyAsync(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId,
        CancellationToken ct = default);
}
