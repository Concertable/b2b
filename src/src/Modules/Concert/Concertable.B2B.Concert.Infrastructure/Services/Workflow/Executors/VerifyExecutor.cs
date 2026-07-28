using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class VerifyExecutor : IVerifyExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IApplicationRepository applicationRepository;
    private readonly IBookingRepository bookingRepository;
    private readonly IConcertNotifier concertNotifier;

    public VerifyExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IApplicationRepository applicationRepository,
        IBookingRepository bookingRepository,
        IConcertNotifier concertNotifier)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.applicationRepository = applicationRepository;
        this.bookingRepository = bookingRepository;
        this.concertNotifier = concertNotifier;
    }

    public async Task ExecuteAsync(int applicationId, string transactionId)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId).OrNotFound();
        application.RecordPaymentVerified(transactionId);
        await applicationRepository.SaveChangesAsync();

        await ConvergeAsync(applicationId);
    }

    public async Task ExecuteFailedAsync(int applicationId, string venueManagerId, string? failureMessage)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId).OrNotFound();
        application.RecordPaymentFailed(null);

        if (application.State != LifecycleState.Cancelled)
            await concertNotifier.VerifyPaymentFailedAsync(venueManagerId, new { applicationId = application.Id, FailureMessage = failureMessage });

        await applicationRepository.SaveChangesAsync();

        await ConvergeAsync(applicationId);
    }

    public Task ConvergeAfterAcceptAsync(int applicationId) => ConvergeAsync(applicationId);

    private async Task ConvergeAsync(int applicationId)
    {
        var snapshot = await applicationRepository.GetConvergenceSnapshotAsync(applicationId);
        if (snapshot is not { } join || !IsBookingPending(join.State))
            return;

        try
        {
            await (join.Verification switch
            {
                PaymentVerification.Verified => BookAsync(applicationId),
                PaymentVerification.Failed => FailAsync(applicationId),
                _ => Task.CompletedTask,
            });
        }
        catch (ConflictException)
        {
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
        }
    }

    private Task BookAsync(int applicationId)
        => transitioner.TransitionAsync(applicationId, Trigger.VerifyPaymentSucceeded, async app =>
        {
            var booking = await bookingRepository.GetByApplicationIdAsync(app.Id).OrNotFound();
            var workflow = workflows.Create(app.DealType);
            await workflow.Book.ExecuteAsync(booking.Id);
        });

    private Task FailAsync(int applicationId)
        => transitioner.TransitionAsync(applicationId, Trigger.VerifyPaymentFailed);

    private static bool IsBookingPending(LifecycleState state)
        => state is LifecycleState.Accepted or LifecycleState.PaymentFailed;
}
