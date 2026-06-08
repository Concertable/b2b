using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.User.Api.Authorization;

public sealed class VenueManagerAttribute : AuthorizeAttribute
{
    public VenueManagerAttribute()
    {
        Policy = "VenueManager";
    }
}
