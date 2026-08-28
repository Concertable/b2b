using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Composition.Strategies;

public sealed class KeyedStrategyBuilder<TKey>
    where TKey : struct, Enum
{
    private readonly IServiceCollection services;
    private readonly Dictionary<(TKey Key, Type StrategyType), StrategyRegistration> registrations = [];
    private readonly Dictionary<Type, HashSet<TKey>> requiredCoverage = [];

    public KeyedStrategyBuilder(IServiceCollection services)
    {
        this.services = services;
    }

    public KeyStrategyBuilder<TKey> For(TKey key)
    {
        if (!Enum.IsDefined(key))
            throw new InvalidOperationException($"{key} is not a declared {typeof(TKey).Name}.");

        return new KeyStrategyBuilder<TKey>(this, key);
    }

    public KeyedStrategyBuilder<TKey> RequireAll<TStrategy>()
        where TStrategy : class =>
        RequireExactly<TStrategy>(Enum.GetValues<TKey>());

    public KeyedStrategyBuilder<TKey> RequireExactly<TStrategy>(params TKey[] keys)
        where TStrategy : class
    {
        var coverage = keys.ToHashSet();

        if (coverage.Count != keys.Length)
            throw new InvalidOperationException(
                $"Coverage for {typeof(TStrategy).Name} contains duplicate {typeof(TKey).Name} values.");

        var invalid = coverage.Where(key => !Enum.IsDefined(key)).ToArray();
        if (invalid.Length > 0)
            throw new InvalidOperationException(
                $"Coverage for {typeof(TStrategy).Name} contains undeclared {typeof(TKey).Name} values: {string.Join(", ", invalid)}.");

        if (!requiredCoverage.TryAdd(typeof(TStrategy), coverage))
            throw new InvalidOperationException($"Coverage for {typeof(TStrategy).Name} has already been declared.");

        return this;
    }

    public void Build()
    {
        ValidateCoverage();
        ValidateLifetimes();

        foreach (var registration in registrations.Values)
            registration.Add(services);
    }

    internal void Add<TStrategy, TImplementation>(TKey key, ServiceLifetime lifetime)
        where TStrategy : class
        where TImplementation : class, TStrategy
    {
        var registrationKey = (key, typeof(TStrategy));
        if (registrations.ContainsKey(registrationKey))
            throw new InvalidOperationException(
                $"{typeof(TStrategy).Name} already has a registration for {key}.");

        Action<IServiceCollection> add = lifetime switch
        {
            ServiceLifetime.Singleton =>
                collection => collection.AddKeyedSingleton<TStrategy, TImplementation>(key),
            ServiceLifetime.Scoped =>
                collection => collection.AddKeyedScoped<TStrategy, TImplementation>(key),
            _ => throw new InvalidOperationException(
                $"{lifetime} is not a supported keyed strategy lifetime.")
        };

        registrations.Add(
            registrationKey,
            new StrategyRegistration(
                key,
                typeof(TStrategy),
                typeof(TImplementation),
                lifetime,
                add));
    }

    private void ValidateCoverage()
    {
        var undeclared = registrations.Values
            .Select(registration => registration.StrategyType)
            .Distinct()
            .Where(strategyType => !requiredCoverage.ContainsKey(strategyType))
            .ToArray();

        if (undeclared.Length > 0)
            throw new InvalidOperationException(
                $"Coverage has not been declared for: {string.Join(", ", undeclared.Select(type => type.Name))}.");

        foreach (var (strategyType, expected) in requiredCoverage)
        {
            var actual = registrations.Values
                .Where(registration => registration.StrategyType == strategyType)
                .Select(registration => registration.Key)
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
        var conflict = registrations.Values
            .GroupBy(registration => registration.ImplementationType)
            .FirstOrDefault(group => group.Select(registration => registration.Lifetime).Distinct().Skip(1).Any());

        if (conflict is null)
            return;

        var lifetimes = conflict.Select(registration => registration.Lifetime).Distinct();
        throw new InvalidOperationException(
            $"{conflict.Key.Name} has conflicting strategy lifetimes: {string.Join(", ", lifetimes)}.");
    }

    private static string Format(IEnumerable<TKey> keys)
    {
        var values = keys.ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }

    private sealed record StrategyRegistration(
        TKey Key,
        Type StrategyType,
        Type ImplementationType,
        ServiceLifetime Lifetime,
        Action<IServiceCollection> Add);
}

public sealed class KeyStrategyBuilder<TKey>
    where TKey : struct, Enum
{
    private readonly KeyedStrategyBuilder<TKey> builder;
    private readonly TKey key;

    public KeyStrategyBuilder(KeyedStrategyBuilder<TKey> builder, TKey key)
    {
        this.builder = builder;
        this.key = key;
    }

    public KeyStrategyBuilder<TKey> AddSingleton<TStrategy, TImplementation>()
        where TStrategy : class
        where TImplementation : class, TStrategy
    {
        builder.Add<TStrategy, TImplementation>(key, ServiceLifetime.Singleton);
        return this;
    }

    public KeyStrategyBuilder<TKey> AddScoped<TStrategy, TImplementation>()
        where TStrategy : class
        where TImplementation : class, TStrategy
    {
        builder.Add<TStrategy, TImplementation>(key, ServiceLifetime.Scoped);
        return this;
    }
}
