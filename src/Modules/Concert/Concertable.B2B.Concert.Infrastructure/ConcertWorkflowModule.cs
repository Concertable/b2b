using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertWorkflowModule : IConcertWorkflowModule
{
    private readonly ISettlementDispatcher settlementDispatcher;
    private readonly ICompletionDispatcher completionDispatcher;
    private readonly IVerifyDispatcher verifyDispatcher;

    public ConcertWorkflowModule(
        ISettlementDispatcher settlementDispatcher,
        ICompletionDispatcher completionDispatcher,
        IVerifyDispatcher verifyDispatcher)
    {
        this.settlementDispatcher = settlementDispatcher;
        this.completionDispatcher = completionDispatcher;
        this.verifyDispatcher = verifyDispatcher;
    }

    public Task SettleAsync(int bookingId, CancellationToken ct = default)
        => settlementDispatcher.SettleAsync(bookingId);

    public async Task FinishAsync(int concertId, CancellationToken ct = default)
    {
        var result = await completionDispatcher.FinishAsync(concertId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);
    }

    public Task VerifyAsync(int applicationId, CancellationToken ct = default)
        => verifyDispatcher.VerifyAsync(applicationId);
}
