using System.Text.Json.Serialization;

namespace Concertable.Conversations.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageAction
{
    ApplicationReceived,
    ApplicationAccepted,
    ConcertPosted
}
