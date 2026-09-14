using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class VerificationFactory
{
    public static TenantVerificationEntity Approved(Guid tenantId, DateTime now)
    {
        var evidence = VerificationDocumentEntity.Create(tenantId, VerificationDocumentType.CompanyRegistration, ".pdf", now);
        var verification = TenantVerificationEntity.Submit(tenantId, [evidence], now);
        verification.Approve(SeedUsers.Admin, now);
        return verification;
    }
}
