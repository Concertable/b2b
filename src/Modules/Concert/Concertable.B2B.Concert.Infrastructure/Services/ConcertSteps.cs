using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Enums;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class RefundEscrowCancelStep : ICancelStep
{
    private readonly IBus bus;

    public RefundEscrowCancelStep(IBus bus)
    {
        this.bus = bus;
    }

    public Task ExecuteAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        var cancellation = concert.BeginCancellation();
        if (!cancellation.TryGetValue(out var operationId))
            throw new InvalidOperationException($"Concert cannot begin cancellation from {concert.State}.");
        return bus.SendAsync(new RefundEscrowCommand(
            operationId,
            concert.BookingId,
            RefundReasonCodes.RequestedByCustomer), ct);
    }
}

internal sealed class ImmediateCancelStep : ICancelStep
{
    public Task ExecuteAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        if (concert.BeginCancellation().TryGetError(out var beginError))
            throw new InvalidOperationException($"Concert cannot begin cancellation from {beginError.Current}.");
        if (concert.Cancel().TryGetError(out var cancelError))
            throw new InvalidOperationException($"Concert cannot cancel from {cancelError.Current}.");
        return Task.CompletedTask;
    }
}

internal sealed class ReleaseEscrowCompleteStep : ICompleteStep
{
    private readonly IEscrowOperationsClient escrowClient;

    public ReleaseEscrowCompleteStep(IEscrowOperationsClient escrowClient)
    {
        this.escrowClient = escrowClient;
    }

    public async Task<Result<SettlementConfirmation, FinishConcertError>> ExecuteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default)
    {
        var result = await escrowClient.ReleaseByBookingIdAsync(
            settlement.OperationId,
            settlement.BookingId,
            ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.EscrowReleaseFailure(error);

        return new SettlementConfirmation.EscrowReleased();
    }
}

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
        logger.ArtistShareCalculated(settlement.ConcertId, settlement.Gross.Amount);
        logger.SettlingConcert(
            settlement.ConcertId,
            settlement.BookingId,
            settlement.Gross.Amount,
            settlement.PayerTenantId,
            settlement.PayeeTenantId);

        var result = await managerPaymentClient.PayAsync(
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
