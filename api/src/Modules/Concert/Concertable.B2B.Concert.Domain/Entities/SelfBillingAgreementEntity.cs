using System.ComponentModel;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// A per-supplier self-billing agreement — the immutable, e-signed record that authorises Concertable to
/// raise self-billed invoices in the supplier's name. Single-owner scoped to the supplier tenant (unlike the
/// two-party invoice/contract): there is no counterparty on this record. Each grant or renewal is a new row;
/// the tenant's current agreement is the latest whose <see cref="ExpiresAtUtc"/> is in the future (the HMRC
/// ≤12-month review). The supplier's legal identity is frozen at acceptance so the signed agreement states who
/// accepted, as they were then.
/// </summary>
[DisplayName(DisplayNames.SelfBillingAgreement)]
public sealed class SelfBillingAgreementEntity : IIdEntity, ITenantScoped
{
    public int Id { get; private set; }
    public Guid TenantId { get; set; }

    public InvoiceParty Supplier { get; private set; } = null!;
    public ESignature SupplierESignature { get; private set; } = null!;

    public DateTime AcceptedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    public string PlatformTermsVersion { get; private set; } = null!;
    public string ClauseText { get; private set; } = null!;

    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private SelfBillingAgreementEntity() { }

    public static SelfBillingAgreementEntity Create(
        Guid supplierTenantId,
        InvoiceParty supplier,
        ESignature supplierESignature,
        string clauseText,
        string platformTermsVersion,
        DateTime acceptedAtUtc,
        DateTime createdAtUtc) => new()
        {
            TenantId = supplierTenantId,
            Supplier = supplier,
            SupplierESignature = supplierESignature,
            ClauseText = clauseText,
            PlatformTermsVersion = platformTermsVersion,
            AcceptedAtUtc = acceptedAtUtc,
            ExpiresAtUtc = acceptedAtUtc.AddMonths(12),
            CreatedAtUtc = createdAtUtc,
            PdfBlobName = $"self-billing-agreements/{supplierTenantId}-{Guid.NewGuid():N}.pdf"
        };
}
