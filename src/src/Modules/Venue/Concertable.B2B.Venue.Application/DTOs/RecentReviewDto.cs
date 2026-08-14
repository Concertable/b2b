namespace Concertable.B2B.Venue.Application.DTOs;

internal sealed record RecentReviewDto(
    int Id,
    string ReviewerName,
    string? ReviewerAvatarUrl,
    int Stars,
    string? Excerpt,
    DateTimeOffset At,
    string Href);
