using System.Net;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Seed.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

/// <summary>
/// Stamps every seeded application with the artist's e-signature and the terms fingerprint — the pair
/// <c>ApplyExecutor</c> records through the product. The fingerprint comes from the canonical
/// <see cref="ITermsFingerprintCalculator"/> (single source, never reimplemented); it is
/// representation-independent, so the value computed here matches what <c>AcceptExecutor</c> recomputes
/// regardless of decimal scale or DateTimeKind drift between the seed data and a DB round-trip.
/// </summary>
internal static class SeededApplicationSigner
{
    public static async Task SignAsync(
        SeedState seed,
        IDealModule deals,
        ITermsFingerprintCalculator fingerprint,
        DateTime signedAtUtc,
        CancellationToken ct)
    {
        var periodByOpportunityId = seed.Opportunities.ToDictionary(o => o.Id, o => o.Period);
        var dealIdByOpportunityId = seed.Opportunities.ToDictionary(o => o.Id, o => o.DealId);
        var dealById = (await deals.GetByIdsAsync(dealIdByOpportunityId.Values.Distinct(), ct))
            .ToDictionary(c => c.Id);
        var artistById = seed.Artists.ToDictionary(a => a.Id);

        foreach (var application in seed.Applications)
        {
            var artist = artistById[application.ArtistId];
            var deal = dealById[dealIdByOpportunityId[application.OpportunityId]];
            application.RecordArtistESignature(
                new ESignature(artist.UserId, signedAtUtc, IPAddress.Loopback, null, artist.Name, null),
                fingerprint.Calculate(deal, periodByOpportunityId[application.OpportunityId]));
        }
    }
}
