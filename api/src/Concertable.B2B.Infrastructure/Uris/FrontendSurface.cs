namespace Concertable.B2B.Infrastructure.Uris;

/// <summary>
/// Which frontend a generated link belongs to. Business is the neutral surface every tenant can reach
/// whatever it has activated; Venue and Artist are the profile surfaces. A link is addressed to a surface,
/// never to a tenant classification — a business with no marketplace profile still receives invitations.
/// </summary>
public enum FrontendSurface
{
    Business = 1,
    Venue = 2,
    Artist = 3,
}
