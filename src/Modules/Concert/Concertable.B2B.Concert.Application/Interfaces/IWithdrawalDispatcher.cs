namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IWithdrawalDispatcher
{
    Task WithdrawAsync(int applicationId);
}
