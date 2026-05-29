using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.User.Api.Authorization;

public sealed class AuthorizeVenueManagerAttribute : AuthorizeAttribute
{
    public AuthorizeVenueManagerAttribute()
    {
        Policy = "VenueManager";
        Roles = "VenueManager";
    }
}
