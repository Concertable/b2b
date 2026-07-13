using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
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
        IContractModule contracts,
        ITermsFingerprintCalculator fingerprint,
        DateTime signedAtUtc,
        CancellationToken ct)
    {
        var periodByOpportunityId = seed.Opportunities.ToDictionary(o => o.Id, o => o.Period);
        var contractIdByOpportunityId = seed.Opportunities.ToDictionary(o => o.Id, o => o.ContractId);
        var contractById = (await contracts.GetByIdsAsync(contractIdByOpportunityId.Values.Distinct(), ct))
            .ToDictionary(c => c.Id);
        var artistById = seed.Artists.ToDictionary(a => a.Id);

        foreach (var application in seed.Applications)
        {
            var artist = artistById[application.ArtistId];
            var contract = contractById[contractIdByOpportunityId[application.OpportunityId]];
            application.RecordArtistESignature(
                new ESignature(artist.UserId, signedAtUtc, null, null, artist.Name, null),
                fingerprint.Calculate(contract, periodByOpportunityId[application.OpportunityId]));
        }
    }
}
