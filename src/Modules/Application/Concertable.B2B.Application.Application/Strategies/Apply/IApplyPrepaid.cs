using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface IApplyPrepaid : IDealStrategy
{
    ApplicationEntity Apply(
        int artistId,
        int opportunityId,
        DealType dealType,
        string paymentMethodId,
        Guid venueTenantId,
        Guid artistTenantId);
}
