using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.Kernel.Exceptions;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class CancelExecutor : ICancelExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IContractResolver contractResolver;
    private readonly IConcertRepository concertRepository;
    private readonly ILogger<CancelExecutor> logger;

    public CancelExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IContractResolver contractResolver,
        IConcertRepository concertRepository,
        ILogger<CancelExecutor> logger)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.contractResolver = contractResolver;
        this.concertRepository = concertRepository;
        this.logger = logger;
    }

    public async Task<Result> ExecuteAsync(int concertId)
    {
        try
        {
            var concert = await concertRepository.GetByIdWithBookingAsync(concertId)
                .OrNotFound();

            await transitioner.TransitionAsync(concert.Booking.ApplicationId, Trigger.Cancel, async app =>
            {
                await contractResolver.ResolveByConcertIdAsync(concertId);
                var workflow = workflows.Create(app.ContractType);
                await workflow.Cancel.ExecuteAsync(concertId);
                concert.Cancel();
            });
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.FailedToCancelConcert(concertId, ex);
            return Result.Fail(ex.Message);
        }
    }
}
