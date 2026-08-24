using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationAvailabilityProjection : IApplicationAvailabilityProjection
{
    private readonly ApplicationDbContext dbContext;

    public ApplicationAvailabilityProjection(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<bool> OpportunityHasConcertAsync(int opportunityId, CancellationToken ct = default) =>
        dbContext.ConcertAvailabilities.AnyAsync(
            availability => availability.OpportunityId == opportunityId,
            ct);

    public Task<bool> ArtistHasConcertOnDateAsync(
        int artistId,
        DateTime date,
        CancellationToken ct = default) =>
        dbContext.ConcertAvailabilities.AnyAsync(
            availability => availability.ArtistId == artistId && availability.StartDate.Date == date.Date,
            ct);

    public Task<bool> VenueHasConcertOnDateAsync(
        int venueId,
        DateTime date,
        CancellationToken ct = default) =>
        dbContext.ConcertAvailabilities.AnyAsync(
            availability => availability.VenueId == venueId && availability.StartDate.Date == date.Date,
            ct);
}
