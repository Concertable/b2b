using Concertable.B2B.Tenant.Domain.Enums;
using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record VerificationReviewError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var tenantId) =>
            ErrorDefinition.NotFound<NotFound>(
                $"No verification submission was found for tenant {tenantId}."),
        NotPending(var status) =>
            ErrorDefinition.Conflict<NotPending>(
                $"Verification cannot be reviewed while its status is '{status}'.")
    };

    [ErrorCode("tenant.verification_review_not_found")]
    public partial record NotFound(Guid TenantId);

    [ErrorCode("tenant.verification_review_not_pending")]
    public partial record NotPending(TenantVerificationStatus Status);
}
