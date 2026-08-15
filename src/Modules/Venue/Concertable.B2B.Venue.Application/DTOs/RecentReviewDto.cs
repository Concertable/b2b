using System.Text.Json.Serialization;

namespace Concertable.B2B.Venue.Application.DTOs;

internal sealed record RecentReviewDto(
    int Id,
    string ReviewerName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReviewerAvatarUrl,
    int Stars,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Excerpt,
    DateTimeOffset At,
    string Href);
