using Concertable.B2B.Application.Application.Mappers;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class PaymentVerificationRecorder : IPaymentVerificationRecorder
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IUnitOfWorkBehavior unitOfWork;

    public PaymentVerificationRecorder(
        IApplicationRepository applicationRepository,
        IUnitOfWorkBehavior unitOfWork)
    {
        this.applicationRepository = applicationRepository;
        this.unitOfWork = unitOfWork;
    }

    public Task RecordAsync(VerifyPayment payment, CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var application = await applicationRepository
                .GetByIdAsync(payment.ApplicationId, ct)
                .OrNotFound();
            if (!application.RecordPaymentVerification(payment.ToPaymentVerification()))
                return;

            // The verification is stored in its own table but belongs to the application, and an acceptance
            // in flight decides on it. Without this the acceptance commits a booking that contradicts a
            // verification recorded after it read the row, and nothing ever confirms that booking.
            applicationRepository.MarkChanged(application);
        }, ct);
}
