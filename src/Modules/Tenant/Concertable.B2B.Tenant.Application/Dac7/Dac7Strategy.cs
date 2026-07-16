using System.Collections.Frozen;

namespace Concertable.B2B.Tenant.Application.Dac7;

/// <summary>
/// Facade over the per-<see cref="Jurisdiction"/> DAC7 strategies (see <c>api/docs/CODE_PATTERNS.md</c>):
/// maps jurisdiction → concrete strategy in a <see cref="FrozenDictionary{TKey,TValue}"/> and delegates. The
/// DI default for <see cref="IDac7Strategy"/>; concrete strategies are constructor-injected as their concrete
/// types. An unmapped jurisdiction throws <see cref="KeyNotFoundException"/> — a region added to the enum
/// fails loudly until its strategy is registered, never silently borrowing another region's rules.
/// </summary>
internal sealed class Dac7Strategy : IDac7Strategy
{
    private readonly FrozenDictionary<Jurisdiction, IDac7Strategy> strategies;

    public Dac7Strategy(UkDac7Strategy uk) =>
        strategies = new Dictionary<Jurisdiction, IDac7Strategy>
        {
            [Jurisdiction.Gb] = uk,
        }.ToFrozenDictionary();

    public bool IsComplete(Jurisdiction jurisdiction, Compliance? compliance) =>
        strategies[jurisdiction].IsComplete(jurisdiction, compliance);

    public bool IsValidVatNumber(Jurisdiction jurisdiction, string vatNumber) =>
        strategies[jurisdiction].IsValidVatNumber(jurisdiction, vatNumber);

    public string DescribeVatNumberRequirement(Jurisdiction jurisdiction) =>
        strategies[jurisdiction].DescribeVatNumberRequirement(jurisdiction);

    public Dac7FieldLabels GetFieldLabels(Jurisdiction jurisdiction) =>
        strategies[jurisdiction].GetFieldLabels(jurisdiction);
}
