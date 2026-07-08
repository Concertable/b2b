using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The per-contract-type deal-defining numbers that make up the terms fingerprint — the values that
/// must match between the artist's Apply consent and the venue's Accept. Keyed by <c>ContractType</c>
/// (mirrors <see cref="IAgreementTermsRenderer"/>), never a type switch.
/// </summary>
internal interface IContractFingerprintComponent
{
    string Compose(IContract contract);
}
