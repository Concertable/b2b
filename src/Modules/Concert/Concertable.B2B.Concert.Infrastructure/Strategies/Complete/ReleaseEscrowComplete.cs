using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;

internal sealed class ReleaseEscrowComplete : IComplete
{
    private readonly IEscrowOperationsClient escrowOperationsClient;

    public ReleaseEscrowComplete(IEscrowOperationsClient escrowOperationsClient)
    {
        this.escrowOperationsClient = escrowOperationsClient;
    }

    public async Task<Result<SettlementConfirmation, FinishConcertError>> CompleteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default)
    {
        var result = await escrowOperationsClient.ReleaseByBookingIdAsync(
            settlement.OperationId,
            settlement.BookingId,
            ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.EscrowReleaseFailure(error);

        return new SettlementConfirmation.EscrowReleased();
    }
}
