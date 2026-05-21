using System.Text.Json.Serialization;

namespace Concertable.Concert.Domain;

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
