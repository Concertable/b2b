namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record InviteMemberRequest
{
    public required string Email { get; init; }
    public required TenantRole Role { get; init; }
}
