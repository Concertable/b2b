namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record ChangeMemberRolesRequest
{
    public required IReadOnlyList<Guid> RoleIds { get; init; }
}
