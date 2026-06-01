namespace Concertable.B2B.Concert.Contracts;

public sealed record ArtistDashboardCountsDto(
    int PendingApplications,
    int UpcomingConcerts);
