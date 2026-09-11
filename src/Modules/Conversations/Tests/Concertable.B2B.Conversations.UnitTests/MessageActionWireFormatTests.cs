using System.Text.Json;
using System.Text.Json.Serialization;
using Concertable.B2B.Conversations.Contracts.Enums;

namespace Concertable.B2B.Conversations.UnitTests;

public sealed class MessageActionWireFormatTests
{
    private static readonly JsonSerializerOptions options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    [Theory]
    [InlineData(MessageAction.ApplicationReceived, "\"applicationReceived\"")]
    [InlineData(MessageAction.ApplicationAccepted, "\"applicationAccepted\"")]
    [InlineData(MessageAction.ConcertPosted, "\"concertPosted\"")]
    [InlineData(MessageAction.ApplicationWithdrawn, "\"applicationWithdrawn\"")]
    [InlineData(MessageAction.ApplicationRejected, "\"applicationRejected\"")]
    [InlineData(MessageAction.ApplicationCancelled, "\"applicationCancelled\"")]
    public void SerializesCamelCase(MessageAction action, string expected)
        => Assert.Equal(expected, JsonSerializer.Serialize(action, options));

    [Theory]
    [InlineData("\"applicationReceived\"", MessageAction.ApplicationReceived)]
    [InlineData("\"concertPosted\"", MessageAction.ConcertPosted)]
    public void DeserializesCamelCase(string json, MessageAction expected)
        => Assert.Equal(expected, JsonSerializer.Deserialize<MessageAction>(json, options));

    [Fact]
    public void RejectsNumericInput()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MessageAction>("0", options));
}
