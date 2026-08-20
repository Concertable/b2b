using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class DealUpdater : IDealUpdater
{
    private readonly IDealStrategyFactory<IDealUpdater> strategies;

    public DealUpdater(IDealStrategyFactory<IDealUpdater> strategies)
    {
        this.strategies = strategies;
    }

    public UnitResult<ValidationErrors> Apply(DealEntity existing, DealDto source)
    {
        if (existing.DealType != source.DealType)
        {
            return new ValidationErrors([
                new(nameof(source.DealType), $"A {source.DealType} deal cannot update a {existing.DealType} deal.")
            ]);
        }

        return strategies.Create(source).Apply(existing, source);
    }
}
