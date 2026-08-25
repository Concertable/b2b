using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class PaymentVerificationRecorder : IPaymentVerificationRecorder
{
    private readonly IApplicationRepository applications;
    private readonly IUnitOfWorkBehavior unitOfWork;

    public PaymentVerificationRecorder(
        IApplicationRepository applications,
        IUnitOfWorkBehavior unitOfWork)
    {
        this.applications = applications;
        this.unitOfWork = unitOfWork;
    }

    public Task RecordAsync(VerifyPayment payment, CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var application = await applications
                .GetForUpdateByIdAsync(payment.ApplicationId, ct)
                .OrNotFound();
            application.RecordVerifyPayment(payment);
        }, ct);
}
