using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class FinishExecutor : IFinishExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IDealResolver dealResolver;
    private readonly IConcertRepository concertRepository;
    private readonly ISettlementPayeeResolver settlementPayeeResolver;
    private readonly ITenantModule tenantModule;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<FinishExecutor> logger;

    public FinishExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IDealResolver dealResolver,
        IConcertRepository concertRepository,
        ISettlementPayeeResolver settlementPayeeResolver,
        ITenantModule tenantModule,
        TimeProvider timeProvider,
        ILogger<FinishExecutor> logger)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.dealResolver = dealResolver;
        this.concertRepository = concertRepository;
        this.settlementPayeeResolver = settlementPayeeResolver;
        this.tenantModule = tenantModule;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<SettlementOutcome>> ExecuteAsync(int concertId)
    {
        try
        {
            var concert = await concertRepository.GetByIdWithBookingAsync(concertId)
                .OrNotFound();
            if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
                throw new BadRequestException("Concert cannot be finished before it has ended");

            // Fail-closed payout gate: the settlement payee's tax identity isn't complete for its jurisdiction →
            // don't transition, don't pay; leave it for the hourly sweep to retry (self-heals once details land).
            var payeeTenantId = settlementPayeeResolver.ResolveTenantId(concert);
            if (!await tenantModule.IsTaxComplianceCompleteAsync(payeeTenantId))
            {
                logger.SettlementDeferredPendingTaxCompliance(concertId, payeeTenantId);
                return Result.Ok(SettlementOutcome.DeferredPendingTaxCompliance);
            }

            await transitioner.TransitionAsync(concert.Booking.ApplicationId, Trigger.Finish, async app =>
            {
                await dealResolver.ResolveByConcertIdAsync(concertId);
                var workflow = workflows.Create(app.DealType);
                await workflow.Finish.ExecuteAsync(concertId);
            });
            return Result.Ok(SettlementOutcome.Settled);
        }
        catch (Exception ex)
        {
            logger.FailedToFinishConcert(concertId, ex);
            return Result.Fail<SettlementOutcome>(ex.Message);
        }
    }
}
