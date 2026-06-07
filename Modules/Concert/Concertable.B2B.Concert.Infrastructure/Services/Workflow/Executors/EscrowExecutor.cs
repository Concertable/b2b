using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class EscrowExecutor : IEscrowExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IBookingRepository bookingRepository;

    public EscrowExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IBookingRepository bookingRepository)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.bookingRepository = bookingRepository;
    }

    public async Task ExecuteAsync(int bookingId)
    {
        var applicationId = await LoadApplicationIdAsync(bookingId);
        await transitioner.TransitionAsync(applicationId, Trigger.EscrowPaymentSucceeded, async app =>
        {
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
        => await bookingRepository.GetApplicationIdByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking not found");
}
