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
    private readonly IConcertNotifier concertNotifier;

    public VerifyExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IBookingRepository bookingRepository,
        IConcertNotifier concertNotifier)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.bookingRepository = bookingRepository;
        this.concertNotifier = concertNotifier;
    }

    public Task ExecuteAsync(int applicationId)
        => transitioner.TransitionAsync(applicationId, Trigger.VerifyPaymentSucceeded, async app =>
        {
            // Verify events ring-fence no money — a late one on a cancelled application is a no-op.
            if (app.State == LifecycleState.Cancelled)
                return;

            var booking = await bookingRepository.GetByApplicationIdAsync(app.Id)
                ?? throw new NotFoundException("Booking not found for application");
            var workflow = workflows.Create(app.ContractType);
            await workflow.Book.ExecuteAsync(booking.Id);
        });

    public Task ExecuteFailedAsync(int applicationId, string venueManagerId, string? failureMessage)
        => transitioner.TransitionAsync(applicationId, Trigger.VerifyPaymentFailed, app =>
            app.State == LifecycleState.Cancelled
                ? Task.CompletedTask
                : concertNotifier.VerifyPaymentFailedAsync(venueManagerId, new { applicationId = app.Id, FailureMessage = failureMessage }));
}
