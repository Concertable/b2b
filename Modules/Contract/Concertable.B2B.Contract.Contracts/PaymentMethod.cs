using System.Text.Json.Serialization;

namespace Concertable.B2B.Contract.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Cash,
    Transfer
}
