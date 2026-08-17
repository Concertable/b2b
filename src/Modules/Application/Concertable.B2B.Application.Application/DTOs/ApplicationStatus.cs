using System.Text.Json.Serialization;

namespace Concertable.B2B.Application.Application.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ApplicationStatus
{
    Pending,
    Rejected,
    Withdrawn,
    Accepted,
    Cancelled
}
