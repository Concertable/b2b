namespace Concertable.Concert.Application.Requests;

internal class BookingParams
{
    public required string PaymentMethodId { get; set; }
    public int ApplicationId { get; set; }
}
