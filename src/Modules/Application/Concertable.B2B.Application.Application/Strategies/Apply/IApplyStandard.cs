using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface IApplyStandard : IDealStrategy
{
    ApplicationEntity Apply(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId);
}
