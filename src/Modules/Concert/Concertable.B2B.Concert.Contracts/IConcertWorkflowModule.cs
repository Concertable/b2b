namespace Concertable.B2B.Concert.Contracts;

public interface IConcertWorkflowModule
{
    Task VerifySucceededAsync(int applicationId, CancellationToken ct = default);
    Task EscrowSucceededAsync(int bookingId, CancellationToken ct = default);
    Task SettlementSucceededAsync(int bookingId, CancellationToken ct = default);
    Task FinishAsync(int concertId, CancellationToken ct = default);
}
