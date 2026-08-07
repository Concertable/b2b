using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Artist.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateArtistError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Forbidden =>
            ErrorDefinition.Forbidden<Forbidden>(
                "No active organization was found for the current user.")
    };

    [ErrorCode("artist.create_forbidden")]
    public partial record Forbidden;
}
