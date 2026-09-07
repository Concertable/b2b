using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.KeyedStrategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Infrastructure.Services.Strategies;

internal sealed class DealUnionFactory<TUnion> : IDealUnionFactory<TUnion>
{
    private readonly IKeyedServiceProvider serviceProvider;
    private readonly KeyedUnionCatalog<DealType, TUnion> catalog;

    public DealUnionFactory(
        IKeyedServiceProvider serviceProvider,
        KeyedUnionCatalog<DealType, TUnion> catalog)
    {
        this.serviceProvider = serviceProvider;
        this.catalog = catalog;
    }

    public TUnion Create(DealType dealType)
    {
        var caseType = catalog.GetCaseType(dealType);
        var value = serviceProvider.GetRequiredKeyedService(caseType, dealType);
        return catalog.Create(dealType, value);
    }
}
