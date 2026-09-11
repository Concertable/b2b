using System.Text.Json;
using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ManagerConcertCardTests
{
    [Fact]
    public void Serialize_MissingBanner_OmitsProperty()
    {
        var dto = new ManagerConcertCard(
            1, "Concert", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), "Venue", 10, 100, "/concerts/1");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto));

        Assert.False(json.RootElement.TryGetProperty(nameof(ManagerConcertCard.BannerUrl), out _));
    }
}
