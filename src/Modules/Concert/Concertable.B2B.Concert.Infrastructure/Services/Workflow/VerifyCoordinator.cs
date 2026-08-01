using Concertable.B2B.Concert.Application.Workflow;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class VerifyCoordinator : IVerifyCoordinator
{
    private readonly IPaymentVerificationRecorder recorder;
    private readonly IBookingAdvancer bookingAdvancer;

    public VerifyCoordinator(
        IPaymentVerificationRecorder recorder,
        IBookingAdvancer bookingAdvancer)
    {
        this.recorder = recorder;
        this.bookingAdvancer = bookingAdvancer;
    }

    public async Task SucceededAsync(int applicationId, CancellationToken ct = default)
    {
        await recorder.RecordVerifiedAsync(applicationId, ct);
        await bookingAdvancer.AdvanceIfReadyAsync(applicationId, ct);
    }

    public async Task FailedAsync(
        int applicationId,
        string venueManagerId,
        string? failureMessage,
        CancellationToken ct = default)
    {
        await recorder.RecordFailedAsync(applicationId, venueManagerId, failureMessage, ct);
        await bookingAdvancer.AdvanceIfReadyAsync(applicationId, ct);
    }
}
