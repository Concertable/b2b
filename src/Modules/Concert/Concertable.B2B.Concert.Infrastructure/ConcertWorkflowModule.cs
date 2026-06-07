using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertWorkflowModule : IConcertWorkflowModule
{
    private readonly IEscrowDispatcher escrowDispatcher;
    private readonly ISettlementDispatcher settlementDispatcher;
    private readonly ICompletionDispatcher completionDispatcher;
    private readonly IVerifyDispatcher verifyDispatcher;

    public ConcertWorkflowModule(
        IEscrowDispatcher escrowDispatcher,
        ISettlementDispatcher settlementDispatcher,
        ICompletionDispatcher completionDispatcher,
        IVerifyDispatcher verifyDispatcher)
    {
        this.escrowDispatcher = escrowDispatcher;
        this.settlementDispatcher = settlementDispatcher;
        this.completionDispatcher = completionDispatcher;
        this.verifyDispatcher = verifyDispatcher;
    }

    public Task VerifySucceededAsync(int applicationId, CancellationToken ct = default)
        => verifyDispatcher.VerifySucceededAsync(applicationId);

    public Task EscrowSucceededAsync(int bookingId, CancellationToken ct = default)
        => escrowDispatcher.SucceededAsync(bookingId);

    public Task SettlementSucceededAsync(int bookingId, CancellationToken ct = default)
        => settlementDispatcher.SucceededAsync(bookingId);

    public async Task FinishAsync(int concertId, CancellationToken ct = default)
    {
        var result = await completionDispatcher.FinishAsync(concertId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);
    }
}
