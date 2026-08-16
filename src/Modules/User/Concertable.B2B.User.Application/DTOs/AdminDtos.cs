namespace Concertable.B2B.User.Application.DTOs;

internal sealed record AdminDto(Guid Sub, string Email);

/// <summary>A pending admin invitation for the console's provisioning list. <see cref="ExpiresAt"/> drives the
/// "expires in N days" hint.</summary>
internal sealed record AdminInvitationDto(Guid Id, string Email, DateTime CreatedAt, DateTime ExpiresAt);

internal sealed record AdminOverviewDto(IReadOnlyList<AdminDto> Admins, IReadOnlyList<AdminInvitationDto> PendingInvitations);
