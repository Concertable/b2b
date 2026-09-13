using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.KeyedStrategies;

public sealed class KeyedUnionBuilder<TKey, TUnion>
    where TKey : struct, Enum
{
    private readonly IServiceCollection services;
    private readonly Dictionary<Type, Func<object, TUnion>> cases = [];
    private readonly Dictionary<TKey, UnionRegistration> registrations = [];

    public KeyedUnionBuilder(IServiceCollection services)
    {
        this.services = services;
    }

    public KeyedUnionCaseBuilder<TKey, TUnion, TCase> Case<TCase>(Func<TCase, TUnion> create)
        where TCase : class
    {
        ArgumentNullException.ThrowIfNull(create);

        if (!cases.TryAdd(typeof(TCase), value => create((TCase)value)))
            throw new InvalidOperationException($"{typeof(TCase).Name} is already a declared union case.");

        return new KeyedUnionCaseBuilder<TKey, TUnion, TCase>(this);
    }

    public void Build()
    {
        ValidateCoverage();
        ValidateCases();
        ValidateLifetimes();

        if (services.Any(descriptor =>
                descriptor.ServiceType == typeof(KeyedUnionCatalog<TKey, TUnion>)))
            throw new InvalidOperationException(
                $"A keyed union catalog for {typeof(TUnion).Name} has already been registered.");

        foreach (var registration in registrations.Values)
            registration.Add(services);

        var catalog = registrations.ToDictionary(
            pair => pair.Key,
            pair => new KeyedUnionCase<TUnion>(
                pair.Value.CaseType,
                cases[pair.Value.CaseType]));
        services.AddSingleton(new KeyedUnionCatalog<TKey, TUnion>(catalog));
    }

    internal void Add<TCase, TImplementation>(TKey key, ServiceLifetime lifetime)
        where TCase : class
        where TImplementation : class, TCase
    {
        if (!Enum.IsDefined(key))
            throw new InvalidOperationException($"{key} is not a declared {typeof(TKey).Name}.");

        if (registrations.ContainsKey(key))
            throw new InvalidOperationException(
                $"{typeof(TUnion).Name} already has a union case registration for {key}.");

        Action<IServiceCollection> add = lifetime switch
        {
            ServiceLifetime.Singleton =>
                collection => collection.AddKeyedSingleton<TCase, TImplementation>(key),
            ServiceLifetime.Scoped =>
                collection => collection.AddKeyedScoped<TCase, TImplementation>(key),
            _ => throw new InvalidOperationException(
                $"{lifetime} is not a supported keyed union lifetime.")
        };

        registrations.Add(
            key,
            new UnionRegistration(
                typeof(TCase),
                typeof(TImplementation),
                lifetime,
                add));
    }

    private void ValidateCoverage()
    {
        var expected = Enum.GetValues<TKey>().ToHashSet();
        var actual = registrations.Keys.ToHashSet();
        var missing = expected.Except(actual).ToArray();

        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"Coverage for {typeof(TUnion).Name} is invalid. Missing: {Format(missing)}.");
    }

    private void ValidateCases()
    {
        var undeclared = registrations.Values
            .Select(registration => registration.CaseType)
            .Distinct()
            .Where(caseType => !cases.ContainsKey(caseType))
            .ToArray();
        if (undeclared.Length > 0)
            throw new InvalidOperationException(
                $"Union cases have not been declared for {typeof(TUnion).Name}: " +
                $"{string.Join(", ", undeclared.Select(type => type.Name))}.");

        var inhabited = registrations.Values
            .Select(registration => registration.CaseType)
            .ToHashSet();
        var uninhabited = cases.Keys
            .Where(caseType => !inhabited.Contains(caseType))
            .ToArray();
        if (uninhabited.Length > 0)
            throw new InvalidOperationException(
                $"Union cases have no key registration for {typeof(TUnion).Name}: " +
                $"{string.Join(", ", uninhabited.Select(type => type.Name))}.");

        var overlap = registrations.Values
            .Select(registration => registration.ImplementationType)
            .Distinct()
            .Select(implementationType => new
            {
                ImplementationType = implementationType,
                CaseTypes = cases.Keys
                    .Where(caseType => caseType.IsAssignableFrom(implementationType))
                    .ToArray()
            })
            .FirstOrDefault(candidate => candidate.CaseTypes.Length > 1);
        if (overlap is not null)
            throw new InvalidOperationException(
                $"{overlap.ImplementationType.Name} implements multiple cases of {typeof(TUnion).Name}: " +
                $"{string.Join(", ", overlap.CaseTypes.Select(type => type.Name))}.");
    }

    private void ValidateLifetimes()
    {
        var conflict = registrations.Values
            .GroupBy(registration => registration.ImplementationType)
            .FirstOrDefault(group => group
                .Select(registration => registration.Lifetime)
                .Distinct()
                .Skip(1)
                .Any());

        if (conflict is null)
            return;

        var lifetimes = conflict
            .Select(registration => registration.Lifetime)
            .Distinct();
        throw new InvalidOperationException(
            $"{conflict.Key.Name} has conflicting union lifetimes: {string.Join(", ", lifetimes)}.");
    }

    private static string Format(IEnumerable<TKey> keys)
    {
        var values = keys.ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }

    private sealed record UnionRegistration(
        Type CaseType,
        Type ImplementationType,
        ServiceLifetime Lifetime,
        Action<IServiceCollection> Add);
}

public sealed class KeyedUnionCaseBuilder<TKey, TUnion, TCase>
    where TKey : struct, Enum
    where TCase : class
{
    private readonly KeyedUnionBuilder<TKey, TUnion> builder;

    public KeyedUnionCaseBuilder(KeyedUnionBuilder<TKey, TUnion> builder)
    {
        this.builder = builder;
    }

    public KeyedUnionCaseBuilder<TKey, TUnion, TCase> Use<TImplementation>(params TKey[] keys)
        where TImplementation : class, TCase
    {
        foreach (var key in keys)
            builder.Add<TCase, TImplementation>(key, ServiceLifetime.Singleton);

        return this;
    }

    public KeyedUnionCaseBuilder<TKey, TUnion, TCase> UseScoped<TImplementation>(params TKey[] keys)
        where TImplementation : class, TCase
    {
        foreach (var key in keys)
            builder.Add<TCase, TImplementation>(key, ServiceLifetime.Scoped);

        return this;
    }
}

public sealed class KeyedUnionCatalog<TKey, TUnion>
    where TKey : struct, Enum
{
    private readonly IReadOnlyDictionary<TKey, KeyedUnionCase<TUnion>> cases;

    internal KeyedUnionCatalog(IReadOnlyDictionary<TKey, KeyedUnionCase<TUnion>> cases)
    {
        this.cases = cases;
    }

    public Type GetCaseType(TKey key) => GetCase(key).CaseType;

    public TUnion Create(TKey key, object value) => GetCase(key).Create(value);

    private KeyedUnionCase<TUnion> GetCase(TKey key) =>
        cases.TryGetValue(key, out var unionCase)
            ? unionCase
            : throw new InvalidOperationException(
                $"{key} has no configured case for {typeof(TUnion).Name}.");
}

internal sealed record KeyedUnionCase<TUnion>(
    Type CaseType,
    Func<object, TUnion> Create);
