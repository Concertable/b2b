using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Steps;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class PayoutCompleteStep : ICompleteStep
{
    private readonly IManagerPaymentOperationsClient managerPaymentClient;
    private readonly ILogger<PayoutCompleteStep> logger;

    public PayoutCompleteStep(
        IManagerPaymentOperationsClient managerPaymentClient,
        ILogger<PayoutCompleteStep> logger)
    {
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task<Result<SettlementConfirmation, FinishConcertError>> ExecuteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default)
    {
        this.logger.ArtistShareCalculated(settlement.ConcertId, settlement.Gross.Amount);
        this.logger.SettlingConcert(
            settlement.ConcertId,
            settlement.BookingId,
            settlement.Gross.Amount,
            settlement.PayerTenantId,
            settlement.PayeeTenantId);

        var result = await this.managerPaymentClient.PayAsync(
            settlement.OperationId,
            settlement.PayerTenantId,
            settlement.PayeeTenantId,
            settlement.Gross,
            settlement.PaymentMethodId
                ?? throw new InvalidOperationException(
                    $"Concert {settlement.ConcertId} has no settlement payment method."),
            PaymentSession.OffSession,
            settlement.BookingId,
            ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.ManagerPaymentFailure(error);
        if (!result.TryGetValue(out var outcome) || string.IsNullOrWhiteSpace(outcome.TransactionId))
            throw new InvalidOperationException(
                $"Settlement for concert {settlement.ConcertId} returned no transaction ID.");

        return new SettlementConfirmation.ManagerPaid(outcome.TransactionId);
    }
}
