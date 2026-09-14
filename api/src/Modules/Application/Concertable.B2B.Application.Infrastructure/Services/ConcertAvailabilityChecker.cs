using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ConcertAvailabilityChecker : IConcertAvailabilityChecker
{
    private readonly IApplicationReadDbContext context;

    public ConcertAvailabilityChecker(IApplicationReadDbContext context)
    {
        this.context = context;
    }

    public Task<bool> OpportunityHasConcertAsync(int opportunityId, CancellationToken ct = default) =>
        context.ConcertAvailabilities.AnyAsync(
            availability => availability.OpportunityId == opportunityId,
            ct);

    public Task<bool> ArtistHasConcertOnDateAsync(
        int artistId,
        DateTime date,
        CancellationToken ct = default) =>
        context.ConcertAvailabilities.AnyAsync(
            availability => availability.ArtistId == artistId && availability.StartDate.Date == date.Date,
            ct);

    public Task<bool> VenueHasConcertOnDateAsync(
        int venueId,
        DateTime date,
        CancellationToken ct = default) =>
        context.ConcertAvailabilities.AnyAsync(
            availability => availability.VenueId == venueId && availability.StartDate.Date == date.Date,
            ct);
}
