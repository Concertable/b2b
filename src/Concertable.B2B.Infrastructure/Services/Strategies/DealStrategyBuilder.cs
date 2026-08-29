using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Infrastructure.Extensions;
using Concertable.B2B.KeyedStrategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Infrastructure.Services.Strategies;

public sealed class DealStrategyBuilder
{
    private readonly IServiceCollection services;
    private readonly KeyedStrategyBuilder<DealType> builder;
    private readonly HashSet<Type> requiredStrategies = [];

    public DealStrategyBuilder(IServiceCollection services)
    {
        this.services = services;
        this.builder = new KeyedStrategyBuilder<DealType>(services);
    }

    public DealStrategyKeyBuilder For(DealType dealType) => new(this, builder.For(dealType));

    public void Build()
    {
        builder.Build();
        services.AddDealStrategyFactory();
    }

    internal void RequireAll<TStrategy>()
        where TStrategy : class, IDealStrategy
    {
        if (requiredStrategies.Add(typeof(TStrategy)))
            builder.RequireAll<TStrategy>();
    }
}

public sealed class DealStrategyKeyBuilder
{
    private readonly DealStrategyBuilder builder;
    private readonly KeyStrategyBuilder<DealType> keyedBuilder;

    public DealStrategyKeyBuilder(
        DealStrategyBuilder builder,
        KeyStrategyBuilder<DealType> keyedBuilder)
    {
        this.builder = builder;
        this.keyedBuilder = keyedBuilder;
    }

    public DealStrategyKeyBuilder AddSingleton<TStrategy, TImplementation>()
        where TStrategy : class, IDealStrategy
        where TImplementation : class, TStrategy
    {
        builder.RequireAll<TStrategy>();
        keyedBuilder.AddSingleton<TStrategy, TImplementation>();
        return this;
    }

    public DealStrategyKeyBuilder AddScoped<TStrategy, TImplementation>()
        where TStrategy : class, IDealStrategy
        where TImplementation : class, TStrategy
    {
        builder.RequireAll<TStrategy>();
        keyedBuilder.AddScoped<TStrategy, TImplementation>();
        return this;
    }
}
