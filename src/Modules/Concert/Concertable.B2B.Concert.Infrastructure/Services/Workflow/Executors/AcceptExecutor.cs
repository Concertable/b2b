using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class AcceptExecutor : IAcceptExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IContractResolver contractResolver;
    private readonly IBookingRepository bookingRepository;
    private readonly IBackgroundTaskRunner taskRunner;

    public AcceptExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IContractResolver contractResolver,
        IBookingRepository bookingRepository,
        IBackgroundTaskRunner taskRunner)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.contractResolver = contractResolver;
        this.bookingRepository = bookingRepository;
        this.taskRunner = taskRunner;
    }

    public Task ExecuteAsync(int applicationId, string? paymentMethodId)
        => transitioner.TransitionAsync(applicationId, Trigger.Accept, async app =>
        {
            await contractResolver.ResolveByApplicationIdAsync(app.Id);
            var workflow = workflows.Create(app.ContractType);
            await (workflow switch
            {
                IAcceptsPaid w when paymentMethodId is not null => w.Accept.ExecuteAsync(app.Id, paymentMethodId),
                IAcceptsPaid => throw new BadRequestException("This contract requires a payment method at acceptance"),
                IAcceptsSimple w => w.Accept.ExecuteAsync(app.Id),
                _ => throw new BadRequestException($"Contract {workflow.Type} does not support Accept")
            });

            var booking = await bookingRepository.GetByApplicationIdAsync(app.Id)
                ?? throw new NotFoundException("Booking not found for application");
            app.Accept(booking);

            await taskRunner.RunAsync<IApplicationRepository>(
                (repo, runCt) => repo.RejectAllExceptAsync(app.OpportunityId, app.Id));
        });
}
