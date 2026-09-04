using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Infrastructure.Payments;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class MintEscrowHold : IMintCommitment
{
    public PaymentCommitment Mint(int applicationId, int opportunityId, Guid artistTenantId) =>
        new(
            PaymentCommitmentTokens.EscrowHold,
            PaymentCommitmentCorrelation.ForApplication(applicationId));
}

internal sealed class MintMethodSetup : IMintCommitment
{
    // The artist commits their method before the application row exists, so this one is keyed by the
    // opportunity and the artist and must stay identical from checkout through to the frozen contract.
    public PaymentCommitment Mint(int applicationId, int opportunityId, Guid artistTenantId) =>
        new(
            PaymentCommitmentTokens.MethodSetup,
            PaymentCommitmentCorrelation.ForOpportunityArtist(opportunityId, artistTenantId));
}

internal sealed class MintMethodVerification : IMintCommitment
{
    public PaymentCommitment Mint(int applicationId, int opportunityId, Guid artistTenantId) =>
        new(
            PaymentCommitmentTokens.MethodVerification,
            PaymentCommitmentCorrelation.ForApplication(applicationId));
}
