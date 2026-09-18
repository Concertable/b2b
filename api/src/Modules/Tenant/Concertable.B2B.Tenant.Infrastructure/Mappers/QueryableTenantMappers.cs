namespace Concertable.B2B.Tenant.Infrastructure.Mappers;

internal static class QueryableTenantMappers
{
    extension(IQueryable<TenantEntity> tenants)
    {
        public IQueryable<BusinessFacts> ToBusinessFacts(IQueryable<TenantBusinessProfileEntity> businessProfiles) =>
            tenants.Select(t => new BusinessFacts(
                t.Id,
                t.LegalName,
                t.ContactEmail,
                t.TaxCompliance == null
                    ? null
                    : new TaxComplianceDto
                    {
                        VatNumber = t.TaxCompliance.VatNumber,
                        SellerIdentifier = t.TaxCompliance.SellerIdentifier,
                        RegisteredAddress = new RegisteredAddressDto
                        {
                            Line1 = t.TaxCompliance.RegisteredAddress.Line1,
                            Line2 = t.TaxCompliance.RegisteredAddress.Line2,
                            City = t.TaxCompliance.RegisteredAddress.City,
                            Postcode = t.TaxCompliance.RegisteredAddress.Postcode,
                            Country = t.TaxCompliance.RegisteredAddress.Country,
                        },
                        BankReference = t.TaxCompliance.BankReference,
                        HoldsMusicLicence = t.TaxCompliance.HoldsMusicLicence,
                    },
                businessProfiles
                    .Where(p => p.TenantId == t.Id && p.RetiredAt == null)
                    .Select(p => p.Kind)
                    .ToList(),
                t.AuthorityVersion));
    }
}
