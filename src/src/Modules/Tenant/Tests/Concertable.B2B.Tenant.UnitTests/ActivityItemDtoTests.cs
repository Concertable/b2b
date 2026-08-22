using System.Text.Json;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class ActivityItemDtoTests
{
    [Fact]
    public void Serialize_MissingDetail_OmitsProperty()
    {
        var dto = new ActivityItemDto(
            Guid.NewGuid(), ActivityType.MessageReceived, DateTimeOffset.UtcNow, "Message received", null, "/messages");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto));

        Assert.False(json.RootElement.TryGetProperty(nameof(ActivityItemDto.Detail), out _));
    }
}
