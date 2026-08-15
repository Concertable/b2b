using System.Text.Json;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Deal.Contracts.Enums;

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

    [Fact]
    public void Serialize_MissingVenueAvatar_OmitsProperty()
    {
        var dto = new RecommendedOpportunity(
            1,
            2,
            "Venue",
            null,
            "County",
            "Town",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            [],
            new FlatFeeDeal { PaymentMethod = PaymentMethod.Cash, Fee = 100m },
            100,
            "/opportunities/1");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto));

        Assert.False(json.RootElement.TryGetProperty(nameof(RecommendedOpportunity.VenueAvatarUrl), out _));
    }
}
