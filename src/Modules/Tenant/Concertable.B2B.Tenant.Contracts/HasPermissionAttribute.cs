using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Tenant.Contracts;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        => Policy = PermissionPolicy.Name(permission);
}
