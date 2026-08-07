using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.ValueObjects;

public sealed record TaxCompliance
{
    /// <summary>The seller's VAT number, or null when not VAT-registered. Absence is the registration status —
    /// there is no separate flag to contradict it. Format validity is region-specific (<c>ITaxComplianceRules</c>).</summary>
    public string? VatNumber { get; private init; }
    public string SellerIdentifier { get; private init; } = null!;
    public RegisteredAddress RegisteredAddress { get; private init; } = null!;
    public string BankReference { get; private init; } = null!;

    /// <summary>A self-declared attestation that the tenant holds the live-music licence it is required to
    /// hold; recorded, never verified — the tenant's liability. Every value is valid, so it has no validation
    /// and gates nothing (payouts, bookings, completeness).</summary>
    public bool HoldsMusicLicence { get; private init; }

    private TaxCompliance() { }

    public TaxCompliance(
        string? vatNumber,
        string sellerIdentifier,
        RegisteredAddress registeredAddress,
        string bankReference,
        bool holdsMusicLicence)
    {
        DomainException.ThrowIfNullOrWhiteSpace(sellerIdentifier, "Seller identifier");
        DomainException.ThrowIfNull(registeredAddress, "Registered address");
        DomainException.ThrowIfNullOrWhiteSpace(bankReference, "Bank reference");

        VatNumber = string.IsNullOrWhiteSpace(vatNumber) ? null : vatNumber;
        SellerIdentifier = sellerIdentifier;
        RegisteredAddress = registeredAddress;
        BankReference = bankReference;
        HoldsMusicLicence = holdsMusicLicence;
    }
}
