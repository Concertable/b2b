using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.ValueObjects;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class TenantFactory
{
    public static TenantEntity Create(
        Guid userId,
        string email,
        TenantType type,
        DateTime createdAt,
        bool taxComplianceComplete = true)
    {
        var tenant = TenantEntity.Create(email, userId, type, createdAt, TenantSeedIds.For(userId));
        return !taxComplianceComplete
            ? tenant
            : tenant.UpdateLegalDetails(email, SeedTaxCompliance).Match(
                () => tenant,
                errors => throw new InvalidOperationException(
                    $"Seed tenant {tenant.Id} is invalid: {Format(errors)}"));
    }

    private static TaxCompliance SeedTaxCompliance => RegisteredAddress
        .Create("1 Seed Way", null, "London", "EC1A 1AA", "United Kingdom")
        .Bind(address => TaxCompliance.Create(
            vatNumber: null,
            sellerIdentifier: "SEED000001",
            registeredAddress: address,
            bankReference: "GB00SEED00000000000001",
            holdsMusicLicence: true))
        .Match(
            compliance => compliance,
            errors => throw new InvalidOperationException(
                $"Seed tax compliance is invalid: {Format(errors)}"));

    private static string Format(ValidationErrors errors) => string.Join(
        "; ",
        errors.Errors.SelectMany(error => error.Value.Select(message => $"{error.Key}: {message}")));
}
