using Concertable.B2B.Application.Application.Strategies;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class PrepaidApply : IApplyPrepaid
{
    public ApplicationEntity Apply(
        int artistId,
        int opportunityId,
        DealType dealType,
        string paymentMethodId,
        Guid venueTenantId,
        Guid artistTenantId) =>
        PrepaidApplication.Create(
            artistId,
            opportunityId,
            dealType,
            paymentMethodId,
            venueTenantId,
            artistTenantId);
}
