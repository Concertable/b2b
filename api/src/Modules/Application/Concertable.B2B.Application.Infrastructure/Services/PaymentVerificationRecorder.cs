using Concertable.B2B.Application.Application.Mappers;
namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class PaymentVerificationRecorder : IPaymentVerificationRecorder
{
    private readonly IApplicationPrivilegedRepository applicationRepository;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior;

    public PaymentVerificationRecorder(
        IApplicationPrivilegedRepository applicationRepository,
        IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.applicationRepository = applicationRepository;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task RecordAsync(VerifyPayment payment, CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var application = await applicationRepository.GetByIdForUpdateAsync(payment.ApplicationId, ct)
                ?? throw new InvalidOperationException($"Application {payment.ApplicationId} was not found.");
            if (!application.RecordPaymentVerification(payment.ToPaymentVerification()))
                return;

            applicationRepository.MarkChanged(application);
        }, ct);
}
