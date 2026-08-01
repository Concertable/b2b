namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IBookingAdvancer
{
    Task AdvanceIfReadyAsync(int applicationId, CancellationToken ct = default);
}
