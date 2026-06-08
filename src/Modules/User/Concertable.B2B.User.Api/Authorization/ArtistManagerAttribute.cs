using Microsoft.AspNetCore.Authorization;

namespace Concertable.B2B.User.Api.Authorization;

public sealed class ArtistManagerAttribute : AuthorizeAttribute
{
    public ArtistManagerAttribute()
    {
        Policy = "ArtistManager";
    }
}
