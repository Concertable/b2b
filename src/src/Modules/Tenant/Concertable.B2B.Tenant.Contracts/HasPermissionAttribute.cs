using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>Requires the active membership to grant the specified permission.</summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        => Policy = PermissionPolicy.Name(permission);
}
