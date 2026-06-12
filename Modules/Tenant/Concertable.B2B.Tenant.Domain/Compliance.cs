using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain;

public sealed record Compliance
{
    public bool VatRegistered { get; private init; }
    public string? VatNumber { get; private init; }
    public string SellerIdentifier { get; private init; } = null!;
    public RegisteredAddress RegisteredAddress { get; private init; } = null!;
    public string BankReference { get; private init; } = null!;

    private Compliance() { }

    public Compliance(
        bool vatRegistered,
        string? vatNumber,
        string sellerIdentifier,
        RegisteredAddress registeredAddress,
        string bankReference)
    {
        if (vatRegistered && string.IsNullOrWhiteSpace(vatNumber))
            throw new DomainException("VAT number is required for a VAT-registered organization.");
        if (!vatRegistered && !string.IsNullOrWhiteSpace(vatNumber))
            throw new DomainException("VAT number must be empty when not VAT-registered.");
        DomainException.ThrowIfNullOrWhiteSpace(sellerIdentifier, "Seller identifier");
        DomainException.ThrowIfNull(registeredAddress, "Registered address");
        DomainException.ThrowIfNullOrWhiteSpace(bankReference, "Bank reference");

        VatRegistered = vatRegistered;
        VatNumber = vatRegistered ? vatNumber : null;
        SellerIdentifier = sellerIdentifier;
        RegisteredAddress = registeredAddress;
        BankReference = bankReference;
    }
}
