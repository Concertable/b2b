using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class MintEscrowHold : IMintCommitment
{
    public PaymentOperationReference Mint(int applicationId, int opportunityId, Guid artistTenantId) =>
        PaymentOperationReferences.EscrowHold(applicationId);
}

internal sealed class MintMethodSetup : IMintCommitment
{
    // The artist commits their method before the application row exists, so this one is keyed by the
    // opportunity and the artist and must stay identical from checkout through to the frozen contract.
    public PaymentOperationReference Mint(int applicationId, int opportunityId, Guid artistTenantId) =>
        PaymentOperationReferences.MethodSetup(opportunityId, artistTenantId);
}

internal sealed class MintMethodVerification : IMintCommitment
{
    public PaymentOperationReference Mint(int applicationId, int opportunityId, Guid artistTenantId) =>
        PaymentOperationReferences.MethodVerification(applicationId);
}
