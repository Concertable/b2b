namespace Concertable.B2B.Venue.Infrastructure;

internal sealed class VenueModule : IVenueModule
{
    private readonly IVenueRepository repo;
    private readonly IPublicVenueRepository publicRepo;

    public VenueModule(IVenueRepository repo, IPublicVenueRepository publicRepo)
    {
        this.repo = repo;
        this.publicRepo = publicRepo;
    }

    public Task<VenueSummary?> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        publicRepo.GetSummaryAsync(venueId);

    public Task<int?> GetVenueIdByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        repo.GetIdByUserIdAsync(userId);
}
