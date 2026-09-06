namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// A row whose cancellation is a claimable long-running operation, such as a booking or a concert whose
/// cancellation drives a refund. Lets a financial outcome be correlated back to the row that started it
/// without the caller knowing which aggregate it is.
/// </summary>
public interface IHasCancellationClaim
{
    OperationClaim Cancellation { get; }
}
