using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.User.Api.Authorization;

public sealed class AuthorizeAdminAttribute : AuthorizeAttribute
{
    public AuthorizeAdminAttribute()
    {
        Policy = "Admin";
        Roles = "Admin";
    }
}
