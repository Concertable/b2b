using Concertable.B2B.Application.Application.Errors;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Strategies;

internal sealed class VenueHireApplyStep : IApplyStep
{
    private readonly IPaymentSessionOperationsClient paymentSessions;

    public VenueHireApplyStep(IPaymentSessionOperationsClient paymentSessions)
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
        var reference = PaymentOperationReferences.MethodSetup(opportunityId, artistTenantId);
        var validation = await paymentSessions.ValidatePaymentMethodAsync(
            new PaymentMethodValidationRequest(reference, artistTenantId), ct);
        if (validation.IsFailure)
            return new ApplyApplicationError.PaymentCommitmentMissing();

        return ApplicationEntity.Create(artistId, opportunityId, dealType, venueTenantId, artistTenantId);
    }
}
