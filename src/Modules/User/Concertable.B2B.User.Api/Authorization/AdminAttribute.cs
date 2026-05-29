using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.User.Api.Authorization;

public sealed class AdminAttribute : AuthorizeAttribute
{
    public AdminAttribute()
    {
        Policy = "Admin";
    }
}
