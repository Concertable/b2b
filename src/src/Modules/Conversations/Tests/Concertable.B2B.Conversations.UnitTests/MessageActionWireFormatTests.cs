using System.Text.Json;
using Concertable.B2B.Conversations.Contracts.Enums;

namespace Concertable.B2B.Conversations.UnitTests;

public sealed class MessageActionWireFormatTests
{
    [Theory]
    [InlineData(MessageAction.ApplicationReceived, "\"applicationReceived\"")]
    [InlineData(MessageAction.ApplicationAccepted, "\"applicationAccepted\"")]
    [InlineData(MessageAction.ConcertPosted, "\"concertPosted\"")]
    [InlineData(MessageAction.ApplicationWithdrawn, "\"applicationWithdrawn\"")]
    [InlineData(MessageAction.ApplicationRejected, "\"applicationRejected\"")]
    [InlineData(MessageAction.ApplicationCancelled, "\"applicationCancelled\"")]
    public void SerializesCamelCase(MessageAction action, string expected)
        => Assert.Equal(expected, JsonSerializer.Serialize(action));

    [Theory]
    [InlineData("\"applicationReceived\"", MessageAction.ApplicationReceived)]
    [InlineData("\"concertPosted\"", MessageAction.ConcertPosted)]
    public void DeserializesCamelCase(string json, MessageAction expected)
        => Assert.Equal(expected, JsonSerializer.Deserialize<MessageAction>(json));
}
