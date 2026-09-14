using System.Text.Json.Serialization;

namespace Concertable.B2B.Venue.Api.Responses;

internal sealed record RecentReviewResponse(
    int Id,
    string ReviewerName,
    int Stars,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Excerpt,
    DateTimeOffset At,
    string Href);
