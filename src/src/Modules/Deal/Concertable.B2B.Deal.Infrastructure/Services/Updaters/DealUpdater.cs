using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class DealUpdater : IDealUpdater
{
    private readonly IDealStrategyFactory<IDealUpdater> strategies;

    public DealUpdater(IDealStrategyFactory<IDealUpdater> strategies)
    {
        this.strategies = strategies;
    }

    public void Apply(DealEntity existing, IDeal source) =>
        strategies.Create(source.DealType).Apply(existing, source);
}
