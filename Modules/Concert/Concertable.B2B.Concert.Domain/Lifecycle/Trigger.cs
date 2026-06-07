namespace Concertable.B2B.Concert.Domain.Lifecycle;

public enum Trigger
{
    Accept,
    Reject,
    Withdraw,
    VerifyPaymentSucceeded,
    VerifyPaymentFailed,
    EscrowPaymentSucceeded,
    EscrowPaymentFailed,
    SettlementPaymentSucceeded,
    SettlementPaymentFailed,
    Finish,
}
