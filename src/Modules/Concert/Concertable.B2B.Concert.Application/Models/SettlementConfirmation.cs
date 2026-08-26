namespace Concertable.B2B.Concert.Application.Models;

internal abstract record SettlementConfirmation
{
    internal sealed record EscrowReleased : SettlementConfirmation;
    internal sealed record ManagerPaid(string TransactionId) : SettlementConfirmation;
}
