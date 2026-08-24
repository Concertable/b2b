namespace Concertable.B2B.Tenant.Domain.Enums;

/// <summary>The state-machine triggers governing <see cref="Entities.TenantVerificationEntity"/>'s transitions.</summary>
public enum TenantVerificationTrigger
{
    Approve,
    Reject,
    Resubmit
}
