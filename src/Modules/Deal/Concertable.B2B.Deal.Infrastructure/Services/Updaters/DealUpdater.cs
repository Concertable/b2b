using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Infrastructure.Services.Updaters;

internal sealed class DealUpdater : IDealUpdater
{
    private readonly IDealStrategyFactory<IDealUpdater> factory;

    public DealUpdater(IDealStrategyFactory<IDealUpdater> factory)
    {
        this.factory = factory;
    }

    public UnitResult<ValidationErrors> Apply(DealEntity existing, DealDto source)
    {
        if (existing.DealType != source.DealType)
        {
            return new ValidationErrors([
                new(nameof(source.DealType), $"A {source.DealType} deal cannot update a {existing.DealType} deal.")
            ]);
        }

        return factory.Create(source.DealType).Apply(existing, source);
    }
}
