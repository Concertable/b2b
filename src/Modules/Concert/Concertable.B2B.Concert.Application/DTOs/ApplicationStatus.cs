using System.Text.Json.Serialization;

namespace Concertable.B2B.Concert.Application.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationStatus
{
    Pending,
    Rejected,
    Withdrawn,
    Accepted
}
