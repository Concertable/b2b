using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertWorkflowModule(
    ISettlementDispatcher SettlementDispatcher,
    ICompletionDispatcher CompletionDispatcher,
    IVerifyDispatcher VerifyDispatcher) : IConcertWorkflowModule
{
    public Task SettleAsync(int bookingId, CancellationToken ct = default)
        => SettlementDispatcher.SettleAsync(bookingId);

    public async Task FinishAsync(int concertId, CancellationToken ct = default)
    {
        var result = await CompletionDispatcher.FinishAsync(concertId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);
    }

    public Task VerifyAsync(int applicationId, CancellationToken ct = default)
        => VerifyDispatcher.VerifyAsync(applicationId);
}
