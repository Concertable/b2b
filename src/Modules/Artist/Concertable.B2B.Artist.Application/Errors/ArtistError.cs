using Concertable.Kernel.Errors;

namespace Concertable.B2B.Artist.Application.Errors;

internal sealed record ArtistError(ErrorDefinition Definition) : IError
{
    internal static ArtistError NotFound(int artistId) =>
        new(ErrorDefinition.NotFound(
            "artist.get.not_found",
            $"Artist {artistId} was not found."));

    internal static readonly ArtistError CurrentTenantNotFound = new(
        ErrorDefinition.NotFound(
            "artist.get.current_tenant_not_found",
            "No artist was found for the current tenant."));
}
