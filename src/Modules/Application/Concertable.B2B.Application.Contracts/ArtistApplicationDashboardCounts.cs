namespace Concertable.B2B.Application.Contracts;

public sealed record ArtistApplicationDashboardCounts(
    int PendingApplications,
    int AcceptedAwaitingCheckout);
