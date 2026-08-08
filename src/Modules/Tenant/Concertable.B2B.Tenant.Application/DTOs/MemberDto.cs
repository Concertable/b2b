namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>
/// A tenant member for the members-management list. Membership stores only <see cref="UserId"/>; the
/// <see cref="Email"/> is resolved from the User projection for display.
/// </summary>
internal sealed record MemberDto(Guid UserId, string Email, TenantRole Role);
