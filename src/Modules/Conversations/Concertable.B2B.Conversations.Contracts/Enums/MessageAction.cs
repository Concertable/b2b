using System.Text.Json.Serialization;

namespace Concertable.B2B.Conversations.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<MessageAction>))]
public enum MessageAction
{
    [JsonStringEnumMemberName("applicationReceived")]
    ApplicationReceived,
    [JsonStringEnumMemberName("applicationAccepted")]
    ApplicationAccepted,
    [JsonStringEnumMemberName("concertPosted")]
    ConcertPosted,
    [JsonStringEnumMemberName("applicationWithdrawn")]
    ApplicationWithdrawn,
    [JsonStringEnumMemberName("applicationRejected")]
    ApplicationRejected,
    [JsonStringEnumMemberName("applicationCancelled")]
    ApplicationCancelled
}
