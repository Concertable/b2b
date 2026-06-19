using Concertable.B2B.Tenant.Application.DTOs;

namespace Concertable.B2B.Tenant.Application.Mappers;

internal static class TenantMappers
{
    public static TenantDto ToDto(this TenantEntity tenant) =>
        new(tenant.Id, tenant.LegalName);

    public static TenantDetails ToDetails(this TenantEntity tenant) => new()
    {
        Id = tenant.Id,
        LegalName = tenant.LegalName,
        Compliance = tenant.Compliance?.ToDto(),
    };

    public static ComplianceDto ToDto(this Compliance compliance) => new()
    {
        VatRegistered = compliance.VatRegistered,
        VatNumber = compliance.VatNumber,
        SellerIdentifier = compliance.SellerIdentifier,
        RegisteredAddress = compliance.RegisteredAddress.ToDto(),
        BankReference = compliance.BankReference,
    };

    public static RegisteredAddressDto ToDto(this RegisteredAddress address) => new()
    {
        Line1 = address.Line1,
        Line2 = address.Line2,
        City = address.City,
        Postcode = address.Postcode,
        Country = address.Country,
    };

    public static Compliance ToCompliance(this ComplianceDto dto) => new(
        dto.VatRegistered,
        dto.VatNumber,
        dto.SellerIdentifier,
        new RegisteredAddress(
            dto.RegisteredAddress.Line1,
            dto.RegisteredAddress.Line2,
            dto.RegisteredAddress.City,
            dto.RegisteredAddress.Postcode,
            dto.RegisteredAddress.Country),
        dto.BankReference);
}
