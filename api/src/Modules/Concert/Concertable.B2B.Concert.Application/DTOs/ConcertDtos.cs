using System.ComponentModel;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

[DisplayName(DisplayNames.Concert)]
internal sealed record ConcertOperations
{
    public int Id { get; init; }
    public int ApplicationId { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public string? BannerUrl { get; init; }
    public string? Avatar { get; init; }
    public double Rating { get; init; }
    public int TotalTickets { get; init; }
    public int AvailableTickets { get; init; }
    public decimal Price { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public ConcertState State { get; init; }
    public bool CanCancel { get; init; }
    public required ConcertVenue Venue { get; init; }
    public required ConcertArtist Artist { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

internal sealed record ConcertDraftReference(int Id, int ApplicationId);

internal sealed record ConcertVenue
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public double Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

internal sealed record ConcertArtist
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public double Rating { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

internal sealed record ConcertSummary(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string VenueName,
    string ArtistName,
    ConcertState State);

internal sealed record ConcertFinance(
    int Id,
    int TicketsSold,
    decimal? DoorRevenue,
    bool IsRevenueShare,
    int? InvoiceId,
    bool CanDeclareDoorRevenue);

internal sealed record ConcertDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string ImageUrl { get; init; }
    public double? Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public IReadOnlyList<Genre> Genres { get; init; } = [];
}
