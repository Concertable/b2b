using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class VerifyExecutor : IVerifyExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IBookingRepository bookingRepository;

    public VerifyExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IBookingRepository bookingRepository)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.bookingRepository = bookingRepository;
    }

    public Task ExecuteAsync(int applicationId)
        => transitioner.TransitionAsync(applicationId, Trigger.VerifyPaymentSucceeded, async app =>
        {
            var booking = await bookingRepository.GetByApplicationIdAsync(app.Id).OrNotFound();
            var workflow = workflows.Create(app.DealType);
            await workflow.Book.ExecuteAsync(booking.Id);
        });

    public Task ExecuteFailedAsync(int applicationId)
        => transitioner.TransitionAsync(applicationId, Trigger.VerifyPaymentFailed);
}
