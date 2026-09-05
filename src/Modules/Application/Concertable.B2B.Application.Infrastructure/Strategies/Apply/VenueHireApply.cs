using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class VenueHireApply : IApply
{
    private readonly IPaymentSessionOperationsClient paymentSessions;

    public VenueHireApply(IPaymentSessionOperationsClient paymentSessions)
    {
        this.paymentSessions = paymentSessions;
    }

    public async Task<Result<ApplicationEntity, ApplyApplicationError>> ApplyAsync(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId,
        CancellationToken ct = default)
    {
        // The artist commits a payment method before the application row exists, so the commitment is
        // keyed by the opportunity and the artist rather than by an application id.
        var reference = PaymentOperationReferences.MethodSetup(opportunityId, artistTenantId);
        var validation = await paymentSessions.ValidatePaymentMethodAsync(
            new PaymentMethodValidationRequest(reference, artistTenantId), ct);
        if (validation.IsFailure)
            return new ApplyApplicationError.PaymentCommitmentMissing();

        return ApplicationEntity.Create(artistId, opportunityId, dealType, venueTenantId, artistTenantId);
    }
}
