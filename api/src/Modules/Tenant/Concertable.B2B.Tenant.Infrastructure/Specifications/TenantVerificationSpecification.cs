using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Tenant.Infrastructure.Specifications;

internal sealed class TenantVerificationSpecification : SpecificationBuilder<TenantVerificationEntity>
{
    public static ISpecification<TenantVerificationEntity> CreateWithDocuments() =>
        new TenantVerificationSpecification().Include(verification => verification.Documents);
}
