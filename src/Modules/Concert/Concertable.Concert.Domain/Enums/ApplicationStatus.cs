using System.Text.Json.Serialization;

namespace Concertable.Concert.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationStatus
{
    Pending,
    Rejected,
    Withdrawn,
    Accepted
}
