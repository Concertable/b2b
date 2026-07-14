using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Serializes a contract's deal-defining terms to a canonical string — the per-contract-type values
/// that must match between the artist's Apply consent and the venue's Accept, and which
/// <see cref="ITermsFingerprintCalculator"/> hashes into the terms fingerprint. Keyed by
/// <c>ContractType</c>, never a type switch. Distinct from <see cref="IDealTermsRenderer"/>,
/// which produces human-facing presentation text, not a hash input.
/// </summary>
internal interface IDealTermsSerializer
{
    string Serialize(IDeal contract);
}
