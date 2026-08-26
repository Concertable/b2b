namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>Notifies a tenant of an admin decision on their verification submission. Mirrors
/// <c>ContentReportNotifier</c>'s direct-call shape — called straight from the admin service, no domain event.</summary>
internal interface IVerificationNotifier
{
    Task NotifyApprovedAsync(TenantVerificationEntity verification, string? contactEmail);

    Task NotifyRejectedAsync(TenantVerificationEntity verification, string? contactEmail);
}
