using Concertable.Kernel.Errors;

namespace Concertable.B2B.Artist.Application.Errors;

internal sealed record ArtistError : IError
{
    private ArtistError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static ArtistError NotFound(int artistId) =>
        new(ErrorDefinition.NotFound(
            "artist.get.not_found",
            $"Artist {artistId} was not found."));

    internal static ArtistError NotFoundForCurrentTenant() =>
        new(ErrorDefinition.NotFound(
            "artist.get.current_tenant_not_found",
            "No artist was found for the current tenant."));
}
