namespace Concertable.B2B.Concert.Application.Requests;

internal sealed class BookingParams
{
    public required string PaymentMethodId { get; set; }
    public int ApplicationId { get; set; }
}
