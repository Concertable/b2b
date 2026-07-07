using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class WithdrawalDispatcher : IWithdrawalDispatcher
{
    private readonly IWithdrawExecutor executor;

    public WithdrawalDispatcher(IWithdrawExecutor executor)
    {
        this.executor = executor;
    }

    public Task WithdrawAsync(int applicationId) => executor.ExecuteAsync(applicationId);
}
