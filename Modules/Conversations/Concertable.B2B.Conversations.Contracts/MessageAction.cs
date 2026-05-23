using System.Text.Json.Serialization;

namespace Concertable.B2B.Conversations.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageAction
{
    ApplicationReceived,
    ApplicationAccepted,
    ConcertPosted
}
