using System.Net;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Seed.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

/// <summary>
/// Creates the immutable contract snapshots that the product creates when an application is accepted.
/// The dev catalog starts applications beyond that transition, so it must materialize the same directly
/// written aggregate before advertising contract actions for the resulting concerts.
/// </summary>
internal static class SeededContractFactory
{
    public static async Task<IReadOnlyList<ContractEntity>> CreateAsync(
        SeedState seed,
        IReadOnlyCollection<ApplicationEntity> bookedApplications,
        IDealModule deals,
        IDealTermsRenderer termsRenderer,
        string platformTermsVersion,
        DateTime createdAtUtc,
        CancellationToken ct)
    {
        var dealIds = bookedApplications
            .Select(application => application.Opportunity.DealId)
            .Distinct()
            .ToArray();
        var dealById = (await deals.GetByIdsAsync(dealIds, ct)).ToDictionary(deal => deal.Id);
        var artistById = seed.Artists.ToDictionary(artist => artist.Id);
        var venueById = seed.Venues.ToDictionary(venue => venue.Id);

        return bookedApplications.Select(application =>
        {
            var booking = application.Booking!;
            var opportunity = application.Opportunity;
            var artist = artistById[application.ArtistId];
            var venue = venueById[opportunity.VenueId];
            var deal = dealById[opportunity.DealId];

            return ContractEntity.Create(
                booking,
                venue.Id,
                venue.Name,
                artist.Id,
                artist.Name,
                opportunity.Period,
                deal,
                termsRenderer.Render(deal),
                platformTermsVersion,
                application.ArtistESignature,
                new ESignature(
                    venue.UserId,
                    createdAtUtc,
                    IPAddress.Loopback,
                    null,
                    venue.Name,
                    null),
                createdAtUtc);
        }).ToArray();
    }
}
