using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class NameEscrowHold : INameCommitment
{
    public PaymentOperationReference Name(ApplicationEntity application) =>
        PaymentOperationReferences.EscrowHold(application.Id);
}

internal sealed class NameMethodSetup : INameCommitment
{
    public PaymentOperationReference Name(ApplicationEntity application) =>
        PaymentOperationReferences.MethodSetup(application.OpportunityId, application.ArtistTenantId);
}

internal sealed class NameMethodVerification : INameCommitment
{
    public PaymentOperationReference Name(ApplicationEntity application) =>
        PaymentOperationReferences.MethodVerification(application.Id);
}
