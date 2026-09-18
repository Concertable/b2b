using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    public string Permission { get; }
}
