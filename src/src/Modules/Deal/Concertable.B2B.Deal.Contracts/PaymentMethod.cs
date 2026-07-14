using System.Text.Json.Serialization;

namespace Concertable.B2B.Deal.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Cash,
    Transfer
}
