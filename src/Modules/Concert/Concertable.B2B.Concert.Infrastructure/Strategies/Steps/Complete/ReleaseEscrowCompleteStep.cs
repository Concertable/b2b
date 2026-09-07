using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Infrastructure.Payments;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;

internal sealed class ReleaseEscrowCompleteStep : ICompleteStep
{
    private readonly IEscrowOperationsClient escrowOperationsClient;

    public ReleaseEscrowCompleteStep(IEscrowOperationsClient escrowOperationsClient)
    {
        this.escrowOperationsClient = escrowOperationsClient;
    }

    public async Task<UnitResult<FinishConcertError>> CompleteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default)
    {
        var result = await escrowOperationsClient.ReleaseAsync(
            settlement.OperationId,
            PaymentOperationReferences.Escrow(settlement.BookingId),
            ct);
        return result.TryGetError(out var error)
            ? new FinishConcertError.EscrowReleaseFailure(error)
            : new Success();
    }
}
