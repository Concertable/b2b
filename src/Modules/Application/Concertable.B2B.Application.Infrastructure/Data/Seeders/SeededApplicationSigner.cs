using System.Net;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Seed.Infrastructure;

namespace Concertable.B2B.Application.Infrastructure.Data.Seeders;

internal static class SeededApplicationSigner
{
    public static async Task SignAsync(
        SeedState seed,
        IDealModule deals,
        ITermsFingerprintCalculator fingerprint,
        DateTime signedAtUtc,
        CancellationToken ct)
    {
        var periodByOpportunityId = seed.Opportunities.ToDictionary(opportunity => opportunity.Id, opportunity => opportunity.Period);
        var dealIdByOpportunityId = seed.Opportunities.ToDictionary(opportunity => opportunity.Id, opportunity => opportunity.DealId);
        var dealById = (await deals.GetByIdsAsync(dealIdByOpportunityId.Values.Distinct(), ct))
            .ToDictionary(deal => deal.Id);
        var artistById = seed.Artists.ToDictionary(artist => artist.Id);

        foreach (var application in seed.Applications)
        {
            var artist = artistById[application.ArtistId];
            var deal = dealById[dealIdByOpportunityId[application.OpportunityId]];
            application.RecordArtistESignature(
                new Signature(artist.UserId, signedAtUtc, IPAddress.Loopback, null, artist.Name, null),
                fingerprint.Calculate(deal, periodByOpportunityId[application.OpportunityId]));
        }
    }
}
