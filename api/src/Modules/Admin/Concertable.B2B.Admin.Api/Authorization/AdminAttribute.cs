using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.Admin.Api.Authorization;

public sealed class AdminAttribute : AuthorizeAttribute
{
    public AdminAttribute()
    {
        Policy = "Admin";
    }
}
