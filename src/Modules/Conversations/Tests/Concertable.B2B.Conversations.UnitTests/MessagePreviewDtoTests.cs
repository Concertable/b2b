using System.Text.Json;
using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.UnitTests;

public sealed class MessagePreviewDtoTests
{
    [Fact]
    public void Serialize_MissingAvatar_OmitsProperty()
    {
        var dto = new MessagePreviewDto(1, "Venue", null, "Hello", DateTime.UtcNow, false, "/messages/1");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto));

        Assert.False(json.RootElement.TryGetProperty(nameof(MessagePreviewDto.OtherPartyAvatarUrl), out _));
    }
}
