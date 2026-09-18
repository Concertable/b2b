using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Strategies;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class StandardApplyStep : IApplyStep
{
    public Task<Result<ApplicationEntity, ApplyApplicationError>> ApplyAsync(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId,
        DateTime createdAtUtc,
        CancellationToken ct = default) =>
        Task.FromResult<Result<ApplicationEntity, ApplyApplicationError>>(
            ApplicationEntity.Create(
                artistId,
                opportunityId,
                dealType,
                venueTenantId,
                artistTenantId,
                createdAtUtc));
}
