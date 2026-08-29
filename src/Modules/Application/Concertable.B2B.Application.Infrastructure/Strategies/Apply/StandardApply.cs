using Concertable.B2B.Application.Application.Strategies;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class StandardApply : IApplyStandard
{
    public ApplicationEntity Apply(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId) =>
        StandardApplication.Create(
            artistId,
            opportunityId,
            dealType,
            venueTenantId,
            artistTenantId);
}
