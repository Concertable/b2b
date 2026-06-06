using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class VerifyExecutor : IVerifyExecutor
{
    private readonly IWorkflowStateMachine<BookingEntity> stateMachine;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IBookingRepository bookingRepository;

    public VerifyExecutor(
        IWorkflowStateMachine<BookingEntity> stateMachine,
        IConcertWorkflowFactory workflows,
        IBookingRepository bookingRepository)
    {
        this.stateMachine = stateMachine;
        this.workflows = workflows;
        this.bookingRepository = bookingRepository;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException("Booking not found for application");

        await stateMachine.TransitionAsync(booking.Id, ConcertStage.Verified, async b =>
        {
            var workflow = workflows.Create(b.ContractType);
            if (workflow is not IVerifies v)
                throw new BadRequestException($"Contract {workflow.Type} does not support Verify");
            await v.Verify.ExecuteAsync(applicationId);
        });
    }
}
