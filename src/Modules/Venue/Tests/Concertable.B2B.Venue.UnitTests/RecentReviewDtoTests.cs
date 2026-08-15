using System.Text.Json;
using Concertable.B2B.Venue.Application.DTOs;

namespace Concertable.B2B.Venue.UnitTests;

public sealed class RecentReviewDtoTests
{
    [Fact]
    public void Serialize_MissingOptionalFields_OmitsProperties()
    {
        var dto = new RecentReviewDto(1, "Reviewer", null, 5, null, DateTimeOffset.UtcNow, "/reviews/1");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto));

        Assert.False(json.RootElement.TryGetProperty(nameof(RecentReviewDto.ReviewerAvatarUrl), out _));
        Assert.False(json.RootElement.TryGetProperty(nameof(RecentReviewDto.Excerpt), out _));
    }
}
