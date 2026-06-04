namespace Concertable.B2B.Concert.Application.Requests;

internal sealed class BookingParams
{
    public required string PaymentMethodId { get; init; }
    public int ApplicationId { get; init; }
}
