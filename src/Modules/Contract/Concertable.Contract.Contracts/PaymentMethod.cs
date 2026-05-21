using System.Text.Json.Serialization;

namespace Concertable.Contract.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Cash,
    Transfer
}
