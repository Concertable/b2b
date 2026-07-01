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

    public async Task ExecuteAsync(int bookingId)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId);
        await transitioner.TransitionAsync(applicationId, Trigger.SettlementPaymentSucceeded);
    }

    public async Task ExecuteFailedAsync(int bookingId)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId);
        await transitioner.TransitionAsync(applicationId, Trigger.SettlementPaymentFailed);
    }

    private async Task<int> LoadApplicationIdAsync(int bookingId)
        => await bookingRepository.GetApplicationIdByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking not found");
}
