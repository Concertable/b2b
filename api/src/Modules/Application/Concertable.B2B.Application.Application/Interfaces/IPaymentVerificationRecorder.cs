namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IPaymentVerificationRecorder
{
    Task RecordAsync(VerifyPayment payment, CancellationToken ct = default);
}
