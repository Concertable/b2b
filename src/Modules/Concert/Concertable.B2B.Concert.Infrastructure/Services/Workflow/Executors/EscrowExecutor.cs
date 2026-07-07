using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class EscrowExecutor : IEscrowExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IBookingRepository bookingRepository;
    private readonly IApplicationCancelStep cancelStep;

    public EscrowExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IBookingRepository bookingRepository,
        IApplicationCancelStep cancelStep)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.bookingRepository = bookingRepository;
        this.cancelStep = cancelStep;
    }

    public async Task ExecuteAsync(int bookingId)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId);
        await transitioner.TransitionAsync(applicationId, Trigger.EscrowPaymentSucceeded, async app =>
        {
            // A late capture landing after application-cancel confirms money into escrow on a dead
            // application — compensate by refunding instead of booking.
            if (app.State == LifecycleState.Cancelled)
            {
                await cancelStep.ExecuteAsync(app.Id);
                return;
            }

            var workflow = workflows.Create(app.ContractType);
            await workflow.Book.ExecuteAsync(bookingId);
        });
    }

    public async Task ExecuteFailedAsync(int bookingId)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId);
        await transitioner.TransitionAsync(applicationId, Trigger.EscrowPaymentFailed);
    }

    private async Task<int> LoadApplicationIdAsync(int bookingId)
    {
        if (await bookingRepository.GetApplicationIdByIdAsync(bookingId) is { } applicationId)
            return applicationId;
        // Distinguishes a tenant-filter-hidden row from a genuinely-absent one (commit race).
        var existsIgnoringTenant = await bookingRepository.ExistsIgnoringTenantAsync(bookingId);
        throw new NotFoundException($"Booking {bookingId} not found (exists ignoring tenant filter: {existsIgnoringTenant}).");
    }
}
