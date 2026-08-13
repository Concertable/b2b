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
    private readonly IPublicBookingRepository publicBookingRepository;
    private readonly IApplicationCancelStep cancelStep;

    public EscrowExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IBookingRepository bookingRepository,
        IPublicBookingRepository publicBookingRepository,
        IApplicationCancelStep cancelStep)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.bookingRepository = bookingRepository;
        this.publicBookingRepository = publicBookingRepository;
        this.cancelStep = cancelStep;
    }

    public async Task SucceededAsync(int bookingId, CancellationToken ct = default)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId, ct);
        var transition = await transitioner.TransitionAsync<CancelApplicationError>(
            applicationId,
            Trigger.EscrowPaymentSucceeded,
            error => (CancelApplicationError)new CancelApplicationError.TransitionFailure(error),
            async app =>
            {
                // A late capture landing after application-cancel confirms money into escrow on a dead
                // application — compensate by refunding instead of booking.
                if (app.State == LifecycleState.CancellationPending)
                    return await cancelStep.ExecuteAsync(app.Id, ct);

                var workflow = workflows.Create(app.DealType);
                await workflow.Book.ExecuteAsync(bookingId);
                return new Success();
            }, ct);

        if (transition.TryGetError(out var error))
            throw new InvalidOperationException(
                $"Escrow payment handling failed ({error.Definition.Code}): {error.Definition.Message}");
    }

    public async Task FailedAsync(int bookingId, CancellationToken ct = default)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId, ct);
        await transitioner.TransitionAsync(applicationId, Trigger.EscrowPaymentFailed, ct: ct)
            .GetValueOrThrowAsync();
    }

    private async Task<int> LoadApplicationIdAsync(int bookingId, CancellationToken ct)
    {
        if (await bookingRepository.GetApplicationIdByIdAsync(bookingId, ct) is { } applicationId)
            return applicationId;
        // Distinguishes a tenant-filter-hidden row from a genuinely-absent one (commit race).
        var exists = await publicBookingRepository.ExistsAsync(bookingId);
        throw new NotFoundException($"Booking {bookingId} not found (exists ignoring tenant filter: {exists}).");
    }
}
