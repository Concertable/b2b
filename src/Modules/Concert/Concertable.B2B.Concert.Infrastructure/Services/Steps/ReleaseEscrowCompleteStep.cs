using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services;

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
        var result = await this.escrowClient.ReleaseByBookingIdAsync(
            settlement.OperationId,
            settlement.BookingId,
            ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.EscrowReleaseFailure(error);

        return new SettlementConfirmation.EscrowReleased();
    }
}
