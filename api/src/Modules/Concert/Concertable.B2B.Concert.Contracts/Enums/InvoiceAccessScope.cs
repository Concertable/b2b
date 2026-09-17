namespace Concertable.B2B.Concert.Contracts.Enums;

/// <summary>
/// What a grant on an invoice lets a tenant read. The invoice already carries its two frozen legal-side
/// values, so the grant says who may read the document, not who the document is about.
/// </summary>
public enum InvoiceAccessScope
{
    Read = 1,
}
