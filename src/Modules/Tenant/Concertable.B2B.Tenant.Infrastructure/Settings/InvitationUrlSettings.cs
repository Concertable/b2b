namespace Concertable.B2B.Tenant.Infrastructure.Settings;

/// <summary>
/// The manager-portal hosts an invitation accept-link is built against, chosen by the inviting tenant's
/// persona. B2B-owned (not on the shared <c>UrlSettings</c>) because the venue/artist split is a B2B concept.
/// </summary>
internal sealed class InvitationUrlSettings
{
    public string VenueFrontend { get; set; } = null!;
    public string ArtistFrontend { get; set; } = null!;
}
