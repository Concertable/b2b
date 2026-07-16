using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class TenantFactory
{
    // Pass the seed id into Create (not .WithId after) so the raised TenantCreatedDomainEvent carries it.
    // Seed operators are UK; production sources the jurisdiction from config in TenantProvisioningHandler.
    public static TenantEntity Create(Guid userId, string email, TenantType type, DateTime createdAt, bool dac7Complete = true)
    {
        var tenant = TenantEntity.Create(email, userId, type, Jurisdiction.Gb, createdAt, TenantSeedIds.For(userId));
        if (dac7Complete)
            // Onboarded seller — DAC7-complete so the fail-closed payout gate lets settlement through. Pass the
            // email as the legal name so LegalName still carries it to Announce()'s event (Stripe provisioning).
            tenant.UpdateLegalDetails(email, SeedCompliance);
        return tenant;
    }

    private static Compliance SeedCompliance => new(
        vatNumber: null,
        sellerIdentifier: "SEED000001",
        registeredAddress: new RegisteredAddress("1 Seed Way", null, "London", "EC1A 1AA", "United Kingdom"),
        bankReference: "GB00SEED00000000000001");
}
