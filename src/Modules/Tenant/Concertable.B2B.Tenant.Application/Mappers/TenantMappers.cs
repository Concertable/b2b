using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Domain.Errors;

namespace Concertable.B2B.Tenant.Application.Mappers;

internal static class TenantMappers
{
    public static TenantDto ToDto(this TenantEntity tenant) =>
        new(tenant.Id, tenant.LegalName);

    public static TaxComplianceDto ToDto(this TaxCompliance taxCompliance) => new()
    {
        VatNumber = taxCompliance.VatNumber,
        SellerIdentifier = taxCompliance.SellerIdentifier,
        RegisteredAddress = taxCompliance.RegisteredAddress.ToDto(),
        BankReference = taxCompliance.BankReference,
        HoldsMusicLicence = taxCompliance.HoldsMusicLicence,
    };

    public static RegisteredAddressDto ToDto(this RegisteredAddress address) => new()
    {
        Line1 = address.Line1,
        Line2 = address.Line2,
        City = address.City,
        Postcode = address.Postcode,
        Country = address.Country,
    };

    public static Result<TaxCompliance, ValidationErrors> ToTaxCompliance(this TaxComplianceDto? dto)
    {
        if (dto is null)
            return Result.Failure<TaxCompliance, ValidationErrors>(
                new ValidationErrors([new("TaxCompliance", "TaxCompliance is required.")]));

        if (dto.RegisteredAddress is null)
            return Result.Failure<TaxCompliance, ValidationErrors>(
                new ValidationErrors([new("RegisteredAddress", "RegisteredAddress is required.")]));

        return RegisteredAddress.Create(
            dto.RegisteredAddress.Line1,
            dto.RegisteredAddress.Line2,
            dto.RegisteredAddress.City,
            dto.RegisteredAddress.Postcode,
            dto.RegisteredAddress.Country)
        .Bind(address => TaxCompliance.Create(
            dto.VatNumber,
            dto.SellerIdentifier,
            address,
            dto.BankReference,
            dto.HoldsMusicLicence));
    }

    public static AcceptInvitationError ToAcceptInvitationError(
        this InvitationAcceptanceError error) => error switch
        {
            InvitationAcceptanceError.NotPending => new AcceptInvitationError.InvitationNotPending(),
            InvitationAcceptanceError.Expired => new AcceptInvitationError.InvitationExpired()
        };

    public static RevokeInvitationError ToRevokeInvitationError(
        this InvitationRevocationError error) => error switch
        {
            InvitationRevocationError.NotPending => new RevokeInvitationError.InvitationNotPending()
        };
}
