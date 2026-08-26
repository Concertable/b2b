using Concertable.B2B.Tenant.Domain.Enums;
using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record SubmitVerificationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotEligible(var status) =>
            ErrorDefinition.Conflict<NotEligible>(
                $"Verification cannot be submitted while the current status is '{status}'.")
    };

    [ErrorCode("tenant.verification_not_eligible")]
    public partial record NotEligible(TenantVerificationStatus Status);
}
