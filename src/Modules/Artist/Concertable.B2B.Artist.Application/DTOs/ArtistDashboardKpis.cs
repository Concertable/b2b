namespace Concertable.B2B.Artist.Application.DTOs;

internal sealed record ArtistDashboardKpis(
    int PendingApplications,
    int AcceptedAwaitingCheckout,
    int UpcomingConcerts,
    long MtdPayoutsCents);
