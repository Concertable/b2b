using Dunet;

namespace Concertable.B2B.Venue.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateVenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        VenueNotFound(var venueId) =>
            ErrorDefinition.For<UpdateVenueError>().NotFound<VenueNotFound>(
                $"Venue {venueId} was not found.")
    };

    [ErrorCode("venue.update_not_found")]
    public partial record VenueNotFound(int VenueId);
}
