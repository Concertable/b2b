using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class SettlementExecutor : ISettlementExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IBookingRepository bookingRepository;

    public SettlementExecutor(ILifecycleTransitioner transitioner, IBookingRepository bookingRepository)
    {
        this.transitioner = transitioner;
        this.bookingRepository = bookingRepository;
    }

    public async Task SucceededAsync(int bookingId, CancellationToken ct = default)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId, ct);
        await transitioner.TransitionAsync(applicationId, Trigger.SettlementPaymentSucceeded, ct: ct);
    }

    public async Task FailedAsync(int bookingId, CancellationToken ct = default)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId, ct);
        await transitioner.TransitionAsync(applicationId, Trigger.SettlementPaymentFailed, ct: ct);
    }

    private async Task<int> LoadApplicationIdAsync(int bookingId, CancellationToken ct)
        => await bookingRepository.GetApplicationIdByIdAsync(bookingId, ct)
            .OrNotFound(DisplayNames.Booking);
}
