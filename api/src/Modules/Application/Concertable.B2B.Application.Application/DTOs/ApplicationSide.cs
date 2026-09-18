namespace Concertable.B2B.Application.Application.DTOs;

/// <summary>
/// Which side of an application the active tenant is reading it from. The application's own record answers
/// this, not the tenant's business profile: a business that both operates a room and performs holds two
/// profiles and neither one says which side of this application it is on.
/// </summary>
internal enum ApplicationSide
{
    Venue = 1,
    Artist = 2,
}
