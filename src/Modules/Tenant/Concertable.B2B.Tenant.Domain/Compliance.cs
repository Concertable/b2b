using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain;

public sealed record Compliance
{
    /// <summary>The seller's VAT number, or null when not VAT-registered. Absence is the registration status —
    /// there is no separate flag to contradict it. Format validity is jurisdiction-scoped (<c>IDac7Strategy</c>).</summary>
    public string? VatNumber { get; private init; }
    public string SellerIdentifier { get; private init; } = null!;
    public RegisteredAddress RegisteredAddress { get; private init; } = null!;
    public string BankReference { get; private init; } = null!;

    private Compliance() { }

    public Compliance(
        string? vatNumber,
        string sellerIdentifier,
        RegisteredAddress registeredAddress,
        string bankReference)
    {
        DomainException.ThrowIfNullOrWhiteSpace(sellerIdentifier, "Seller identifier");
        DomainException.ThrowIfNull(registeredAddress, "Registered address");
        DomainException.ThrowIfNullOrWhiteSpace(bankReference, "Bank reference");

        VatNumber = string.IsNullOrWhiteSpace(vatNumber) ? null : vatNumber;
        SellerIdentifier = sellerIdentifier;
        RegisteredAddress = registeredAddress;
        BankReference = bankReference;
    }
}
