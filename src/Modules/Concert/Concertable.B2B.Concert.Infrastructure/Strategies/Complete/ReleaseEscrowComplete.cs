using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;

internal sealed class ReleaseEscrowComplete : IComplete
{
    private readonly IEscrowOperationsClient escrowClient;

    public ReleaseEscrowComplete(IEscrowOperationsClient escrowClient)
    {
        this.escrowClient = escrowClient;
    }

    public async Task<Result<SettlementConfirmation, FinishConcertError>> CompleteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default)
    {
        var result = await this.escrowClient.ReleaseByBookingIdAsync(
            settlement.OperationId,
            settlement.BookingId,
            ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.EscrowReleaseFailure(error);

        return new SettlementConfirmation.EscrowReleased();
    }
}
