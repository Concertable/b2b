using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.Infrastructure.Services.Strategies;

internal sealed class BookingDealStrategyBuilder
{
    private readonly IServiceCollection services;
    private readonly Dictionary<(DealType DealType, Type StrategyType), StrategyRegistration> registrations = [];
    private readonly Dictionary<Type, HashSet<DealType>> requiredCoverage = [];

    public BookingDealStrategyBuilder(IServiceCollection services)
    {
        this.services = services;
    }

    public BookingDealTypeStrategyBuilder For(DealType dealType)
    {
        if (!Enum.IsDefined(dealType))
            throw new InvalidOperationException($"{dealType} is not a declared deal type.");

        return new BookingDealTypeStrategyBuilder(this, dealType);
    }

    public BookingDealStrategyBuilder RequireAll<TStrategy>()
        where TStrategy : class =>
        RequireExactly<TStrategy>(Enum.GetValues<DealType>());

    public BookingDealStrategyBuilder RequireExactly<TStrategy>(params DealType[] dealTypes)
        where TStrategy : class
    {
        var coverage = dealTypes.ToHashSet();
        if (coverage.Count != dealTypes.Length)
            throw new InvalidOperationException($"Coverage for {typeof(TStrategy).Name} contains duplicate deal types.");

        var invalid = coverage.Where(dealType => !Enum.IsDefined(dealType)).ToArray();
        if (invalid.Length > 0)
            throw new InvalidOperationException(
                $"Coverage for {typeof(TStrategy).Name} contains undeclared deal types: {string.Join(", ", invalid)}.");

        if (!this.requiredCoverage.TryAdd(typeof(TStrategy), coverage))
            throw new InvalidOperationException($"Coverage for {typeof(TStrategy).Name} has already been declared.");

        return this;
    }

    public void Build()
    {
        ValidateCoverage();
        ValidateLifetimes();
        foreach (var registration in this.registrations.Values)
            registration.Add(this.services);
    }

    internal void Add<TStrategy, TImplementation>(DealType dealType, ServiceLifetime lifetime)
        where TStrategy : class
        where TImplementation : class, TStrategy
    {
        var key = (dealType, typeof(TStrategy));
        if (this.registrations.ContainsKey(key))
            throw new InvalidOperationException(
                $"{typeof(TStrategy).Name} already has a registration for {dealType}.");

        Action<IServiceCollection> add = lifetime switch
        {
            ServiceLifetime.Singleton =>
                collection => collection.AddKeyedSingleton<TStrategy, TImplementation>(dealType),
            ServiceLifetime.Scoped =>
                collection => collection.AddKeyedScoped<TStrategy, TImplementation>(dealType),
            _ => throw new InvalidOperationException(
                $"{lifetime} is not a supported Booking deal strategy lifetime.")
        };

        this.registrations.Add(
            key,
            new StrategyRegistration(
                dealType,
                typeof(TStrategy),
                typeof(TImplementation),
                lifetime,
                add));
    }

    private void ValidateCoverage()
    {
        var undeclared = this.registrations.Values
            .Select(registration => registration.StrategyType)
            .Distinct()
            .Where(strategyType => !this.requiredCoverage.ContainsKey(strategyType))
            .ToArray();
        if (undeclared.Length > 0)
            throw new InvalidOperationException(
                $"Coverage has not been declared for: {string.Join(", ", undeclared.Select(type => type.Name))}.");

        foreach (var (strategyType, expected) in this.requiredCoverage)
        {
            var actual = this.registrations.Values
                .Where(registration => registration.StrategyType == strategyType)
                .Select(registration => registration.DealType)
                .ToHashSet();
            var missing = expected.Except(actual).ToArray();
            var unexpected = actual.Except(expected).ToArray();
            if (missing.Length == 0 && unexpected.Length == 0)
                continue;

            throw new InvalidOperationException(
                $"Coverage for {strategyType.Name} is invalid. " +
                $"Missing: {Format(missing)}. Unexpected: {Format(unexpected)}.");
        }
    }

    private void ValidateLifetimes()
    {
        var conflict = this.registrations.Values
            .GroupBy(registration => registration.ImplementationType)
            .FirstOrDefault(group => group.Select(registration => registration.Lifetime).Distinct().Skip(1).Any());
        if (conflict is null)
            return;

        var lifetimes = conflict.Select(registration => registration.Lifetime).Distinct();
        throw new InvalidOperationException(
            $"{conflict.Key.Name} has conflicting strategy lifetimes: {string.Join(", ", lifetimes)}.");
    }

    private static string Format(IEnumerable<DealType> dealTypes)
    {
        var values = dealTypes.ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }

    private sealed record StrategyRegistration(
        DealType DealType,
        Type StrategyType,
        Type ImplementationType,
        ServiceLifetime Lifetime,
        Action<IServiceCollection> Add);
}

internal sealed class BookingDealTypeStrategyBuilder
{
    private readonly BookingDealStrategyBuilder builder;
    private readonly DealType dealType;

    public BookingDealTypeStrategyBuilder(BookingDealStrategyBuilder builder, DealType dealType)
    {
        this.builder = builder;
        this.dealType = dealType;
    }

    public BookingDealTypeStrategyBuilder AddSingleton<TStrategy, TImplementation>()
        where TStrategy : class
        where TImplementation : class, TStrategy
    {
        this.builder.Add<TStrategy, TImplementation>(this.dealType, ServiceLifetime.Singleton);
        return this;
    }

    public BookingDealTypeStrategyBuilder AddScoped<TStrategy, TImplementation>()
        where TStrategy : class
        where TImplementation : class, TStrategy
    {
        this.builder.Add<TStrategy, TImplementation>(this.dealType, ServiceLifetime.Scoped);
        return this;
    }
}
