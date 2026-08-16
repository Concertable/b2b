using System.Net;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

/// <summary>
/// Grants every seeded operator tenant a current self-billing agreement — the standing consent a supplier holds
/// so its settlements can raise self-billed invoices. Built through the same domain factory, frozen
/// <see cref="InvoiceParty"/> identity (from <see cref="ITenantModule"/>) and supplier e-signature the grant
/// service records, so once Phase 3 turns the fail-closed gate on, seeded settlements are not all deferred. Any
/// tenant can be the settlement payee depending on deal type, so all are granted.
/// </summary>
internal static class SeededSelfBillingAgreementGranter
{
    public static async Task GrantAsync(
        ConcertTenantDbContext context,
        SeedState seed,
        ITenantModule tenants,
        string platformTermsVersion,
        DateTime grantedAtUtc,
        CancellationToken ct)
    {
        foreach (var tenant in seed.Tenants)
        {
            var identityOption = await tenants.GetByIdAsync(tenant.Id, ct);
            var taxOption = await tenants.GetTaxComplianceAsync(tenant.Id, ct);
            if (!identityOption.TryGetValue(out var identity) || !taxOption.TryGetValue(out var tax))
                continue;

            var address = tax.RegisteredAddress;
            var supplier = new InvoiceParty(
                tenant.Id,
                identity.LegalName,
                tax.VatNumber,
                address.Line1,
                address.Line2,
                address.City,
                address.Postcode,
                address.Country);

            var signature = new ESignature(
                tenant.CreatedByUserId, grantedAtUtc, IPAddress.Loopback, null, identity.LegalName, null);

            context.SelfBillingAgreements.Add(SelfBillingAgreementEntity.Create(
                tenant.Id,
                supplier,
                signature,
                SelfBillingClause.Render(identity.LegalName),
                platformTermsVersion,
                grantedAtUtc,
                grantedAtUtc));
        }
    }
}
