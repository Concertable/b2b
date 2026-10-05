namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record InviteMemberRequest
{
    public required string Email { get; init; }
    public required IReadOnlyList<Guid> RoleIds { get; init; }
}
