namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record ChangeMemberRoleRequest
{
    public required TenantRole Role { get; init; }
}
