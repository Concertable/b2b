namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// A deal type's human-readable presentation and canonical fingerprint input. The two representations
/// remain separate even though they share one deal-type selection.
/// </summary>
internal interface IDealTerms
{
    string Render(IDeal deal);

    string Serialize(IDeal deal);
}
