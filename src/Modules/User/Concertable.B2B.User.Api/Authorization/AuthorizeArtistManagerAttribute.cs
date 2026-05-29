using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.User.Api.Authorization;

public sealed class AuthorizeArtistManagerAttribute : AuthorizeAttribute
{
    public AuthorizeArtistManagerAttribute()
    {
        Policy = "ArtistManager";
        Roles = "ArtistManager";
    }
}
