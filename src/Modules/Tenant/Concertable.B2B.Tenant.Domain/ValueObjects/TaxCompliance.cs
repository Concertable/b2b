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

    private TaxCompliance(
        string? vatNumber,
        string sellerIdentifier,
        RegisteredAddress registeredAddress,
        string bankReference,
        bool holdsMusicLicence)
    {
        VatNumber = string.IsNullOrWhiteSpace(vatNumber) ? null : vatNumber;
        SellerIdentifier = sellerIdentifier;
        RegisteredAddress = registeredAddress;
        BankReference = bankReference;
        HoldsMusicLicence = holdsMusicLicence;
    }

    public static Result<TaxCompliance, ValidationErrors> Create(
        string? vatNumber,
        string sellerIdentifier,
        RegisteredAddress? registeredAddress,
        string bankReference,
        bool holdsMusicLicence)
    {
        var errors = new List<KeyValuePair<string, string>>();

        if (vatNumber?.Length > 20)
            errors.Add(new(nameof(VatNumber), "VatNumber must be 20 characters or fewer."));

        ValidateRequired(errors, nameof(SellerIdentifier), sellerIdentifier, 50);

        if (registeredAddress is null)
            errors.Add(new(nameof(RegisteredAddress), "RegisteredAddress is required."));

        ValidateRequired(errors, nameof(BankReference), bankReference, 50);

        return errors.Count == 0
            ? Result.Success<TaxCompliance, ValidationErrors>(
                new TaxCompliance(
                    vatNumber,
                    sellerIdentifier,
                    registeredAddress!,
                    bankReference,
                    holdsMusicLicence))
            : Result.Failure<TaxCompliance, ValidationErrors>(new ValidationErrors(errors));
    }

    private static void ValidateRequired(
        ICollection<KeyValuePair<string, string>> errors,
        string field,
        string value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(new(field, $"{field} is required."));
        else if (value.Length > maximumLength)
            errors.Add(new(field, $"{field} must be {maximumLength} characters or fewer."));
    }
}
