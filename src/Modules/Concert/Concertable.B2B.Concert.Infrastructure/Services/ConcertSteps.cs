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

    public Task ExecuteAsync(ConcertEntity concert, CancellationToken ct = default) =>
        bus.SendAsync(new RefundEscrowCommand(
            concert.BeginCancellation(),
            concert.BookingId,
            RefundReasonCodes.RequestedByCustomer), ct);
}

internal sealed class ImmediateCancelStep : ICancelStep
{
    public Task ExecuteAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        concert.BeginCancellation();
        concert.Cancel();
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

    public async Task<UnitResult<FinishConcertError>> ExecuteAsync(
        ConcertEntity concert,
        CancellationToken ct = default)
    {
        var result = await escrowClient.ReleaseByBookingIdAsync(concert.BookingId, ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.EscrowReleaseFailure(error);

        concert.CompleteSettlement();
        return new Success();
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

    public async Task<UnitResult<FinishConcertError>> ExecuteAsync(
        ConcertEntity concert,
        CancellationToken ct = default)
    {
        var gross = Money.Gbp(concert.CalculateSettlementGross());
        logger.ArtistShareCalculated(concert.Id, gross.Amount);
        logger.SettlingConcert(
            concert.Id,
            concert.BookingId,
            gross.Amount,
            concert.SettlementPayerTenantId,
            concert.SettlementPayeeTenantId);

        var result = await managerPaymentClient.PayAsync(
            concert.SettlementPayerTenantId,
            concert.SettlementPayeeTenantId,
            gross,
            concert.SettlementPaymentMethodId
                ?? throw new InvalidOperationException($"Concert {concert.Id} has no settlement payment method."),
            PaymentSession.OffSession,
            concert.BookingId,
            ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.ManagerPaymentFailure(error);
        if (!result.TryGetValue(out var outcome) || string.IsNullOrWhiteSpace(outcome.TransactionId))
            throw new InvalidOperationException($"Settlement for concert {concert.Id} returned no transaction ID.");

        concert.BeginSettlement(outcome.TransactionId);
        return new Success();
    }
}
