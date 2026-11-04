using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Authorization.Contracts;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(TenantPermission permission) => Policy = PermissionPolicy.Name(permission);
}
