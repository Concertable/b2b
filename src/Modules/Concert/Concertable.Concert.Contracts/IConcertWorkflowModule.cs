namespace Concertable.Concert.Contracts;

public interface IConcertWorkflowModule
{
    Task SettleAsync(int bookingId, CancellationToken ct = default);
    Task FinishAsync(int concertId, CancellationToken ct = default);
    Task VerifyAsync(int applicationId, CancellationToken ct = default);
}
