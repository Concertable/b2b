using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class EscrowHoldCommitmentReferenceStep : ICommitmentReferenceStep
{
    public PaymentOperationReference Resolve(ApplicationEntity application) =>
        PaymentOperationReferences.EscrowHold(application.Id);
}

internal sealed class MethodSetupCommitmentReferenceStep : ICommitmentReferenceStep
{
    public PaymentOperationReference Resolve(ApplicationEntity application) =>
        PaymentOperationReferences.MethodSetup(application.OpportunityId, application.ArtistTenantId);
}

internal sealed class MethodVerificationCommitmentReferenceStep : ICommitmentReferenceStep
{
    public PaymentOperationReference Resolve(ApplicationEntity application) =>
        PaymentOperationReferences.MethodVerification(application.Id);
}
