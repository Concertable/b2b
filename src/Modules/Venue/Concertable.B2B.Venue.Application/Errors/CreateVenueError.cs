using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateVenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant =>
            ErrorDefinition.Forbidden<NoActiveTenant>(
                "No active organization was found for the current user.")
    };

    [ErrorCode("venue.create_forbidden")]
    public partial record NoActiveTenant;
}
