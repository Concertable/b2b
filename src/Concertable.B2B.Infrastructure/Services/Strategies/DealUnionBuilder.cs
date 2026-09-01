using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Infrastructure.Extensions;
using Concertable.B2B.KeyedStrategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Infrastructure.Services.Strategies;

public sealed class DealUnionBuilder<TUnion>
{
    private readonly IServiceCollection services;
    private readonly KeyedUnionBuilder<DealType, TUnion> builder;

    public DealUnionBuilder(IServiceCollection services)
    {
        this.services = services;
        builder = new KeyedUnionBuilder<DealType, TUnion>(services);
    }

    public DealUnionCaseBuilder<TUnion, TCase> Case<TCase>(Func<TCase, TUnion> create)
        where TCase : class, IDealStrategy =>
        new(builder.Case(create));

    public void Build()
    {
        builder.Build();
        services.AddDealUnionFactory();
    }
}

public sealed class DealUnionCaseBuilder<TUnion, TCase>
    where TCase : class, IDealStrategy
{
    private readonly KeyedUnionCaseBuilder<DealType, TUnion, TCase> builder;

    public DealUnionCaseBuilder(KeyedUnionCaseBuilder<DealType, TUnion, TCase> builder)
    {
        this.builder = builder;
    }

    public DealUnionCaseBuilder<TUnion, TCase> Use<TImplementation>(params DealType[] dealTypes)
        where TImplementation : class, TCase
    {
        builder.Use<TImplementation>(dealTypes);
        return this;
    }

    public DealUnionCaseBuilder<TUnion, TCase> UseScoped<TImplementation>(params DealType[] dealTypes)
        where TImplementation : class, TCase
    {
        builder.UseScoped<TImplementation>(dealTypes);
        return this;
    }
}
