using System.Collections.Frozen;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class DealUpdater : IDealUpdater
{
    private readonly FrozenDictionary<DealType, IDealUpdater> updaters;

    public DealUpdater(
        FlatFeeDealUpdater flatFee,
        DoorSplitDealUpdater doorSplit,
        VersusDealUpdater versus,
        VenueHireDealUpdater venueHire)
    {
        updaters = new Dictionary<DealType, IDealUpdater>
        {
            [DealType.FlatFee] = flatFee,
            [DealType.DoorSplit] = doorSplit,
            [DealType.Versus] = versus,
            [DealType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public void Apply(DealEntity existing, IDeal source) =>
        updaters[source.DealType].Apply(existing, source);
}
