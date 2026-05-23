using System.Text.Json.Serialization;

namespace Concertable.B2B.Concert.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConcertStage
{
    None = 0,
    Applied = 1,
    Verified = 2,
    Accepted = 3,
    Settled = 4,
    Finished = 5
}
