namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A pending invitation for the members-management list. <see cref="Id"/> is the accept token — revoke
/// addresses the invitation by it; <see cref="ExpiresAt"/> drives the "expires in N days" hint.
/// </summary>
public sealed record InvitationDto(Guid Id, string Email, TenantRole Role, DateTime CreatedAt, DateTime ExpiresAt);
