using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// The VAT decomposition an invoice records: the net (ex-VAT) amount, the VAT portion, the VAT-inclusive
/// gross, and the rate applied. Invariant: <c>Net + Vat == Gross</c> — the gross is decomposed, never
/// inflated, so the three figures always balance (an unregistered supplier is <c>Vat == 0, Net == Gross</c>).
/// </summary>
public sealed record VatBreakdown
{
    public decimal Net { get; init; }
    public decimal Vat { get; init; }
    public decimal Gross { get; init; }
    public decimal Rate { get; init; }

    public VatBreakdown(decimal net, decimal vat, decimal gross, decimal rate)
    {
        if (net + vat != gross)
            throw new DomainException($"VAT breakdown does not balance: net {net} + vat {vat} != gross {gross}.");

        Net = net;
        Vat = vat;
        Gross = gross;
        Rate = rate;
    }
}
