using System.Text.Json.Serialization;

namespace Concertable.Concert.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Pending,
    AwaitingPayment,
    Confirmed,
    Complete,
    PaymentFailed
}
