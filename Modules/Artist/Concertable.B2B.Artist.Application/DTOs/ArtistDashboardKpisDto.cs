namespace Concertable.B2B.Artist.Application.DTOs;

public sealed record ArtistDashboardKpisDto(
    int PendingApplications,
    int AcceptedAwaitingCheckout,
    int UpcomingConcerts,
    long MtdPayoutsCents,
    double? MtdPayoutsDeltaPercent);
