using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Executors;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Executors;

internal class SettleExecutor : ISettleExecutor
{
    private readonly IWorkflowStateMachine<BookingEntity> stateMachine;
    private readonly IConcertWorkflowFactory workflows;

    public SettleExecutor(IWorkflowStateMachine<BookingEntity> stateMachine, IConcertWorkflowFactory workflows)
    {
        this.stateMachine = stateMachine;
        this.workflows = workflows;
    }

    public Task ExecuteAsync(int bookingId)
        => stateMachine.TransitionAsync(bookingId, ConcertStage.Settled, async booking =>
        {
            var workflow = workflows.Create(booking.ContractType);
            await workflow.Settle.ExecuteAsync(booking.Id);
        });
}
