using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class PaymentVerificationRecorder : IPaymentVerificationRecorder
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IConcertNotifier concertNotifier;

    public PaymentVerificationRecorder(
        IApplicationRepository applicationRepository,
        IConcertNotifier concertNotifier)
    {
        this.applicationRepository = applicationRepository;
        this.concertNotifier = concertNotifier;
    }

    public async Task RecordVerifiedAsync(int applicationId, CancellationToken ct = default)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct).OrNotFound();
        application.RecordPaymentVerified();
        await applicationRepository.SaveChangesAsync(ct);
    }

    public async Task RecordFailedAsync(
        int applicationId,
        string venueManagerId,
        string? failureMessage,
        CancellationToken ct = default)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId, ct).OrNotFound();
        application.RecordPaymentFailed();

        if (application.State != LifecycleState.Cancelled)
            await concertNotifier.VerifyPaymentFailedAsync(
                venueManagerId,
                new { applicationId = application.Id, FailureMessage = failureMessage },
                ct);

        await applicationRepository.SaveChangesAsync(ct);
    }
}
