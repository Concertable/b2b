using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class VerifyDispatcher : IVerifyDispatcher
{
    private readonly IPaymentVerificationRecorder recorder;
    private readonly IBookingAdvancer bookingAdvancer;

    public VerifyDispatcher(
        IPaymentVerificationRecorder recorder,
        IBookingAdvancer bookingAdvancer)
    {
        this.recorder = recorder;
        this.bookingAdvancer = bookingAdvancer;
    }

    public async Task VerifySucceededAsync(int applicationId)
    {
        await recorder.RecordVerifiedAsync(applicationId);
        await bookingAdvancer.AdvanceIfReadyAsync(applicationId);
    }

    public async Task VerifyFailedAsync(int applicationId, string venueManagerId, string? failureMessage)
    {
        await recorder.RecordFailedAsync(applicationId, venueManagerId, failureMessage);
        await bookingAdvancer.AdvanceIfReadyAsync(applicationId);
    }
}
