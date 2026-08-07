using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record OpportunityMutationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        VenueNotFound =>
            ErrorDefinition.NotFound<VenueNotFound>(
                "No venue was found for the current organization."),
        VenueForbidden =>
            ErrorDefinition.Forbidden<VenueForbidden>("You do not own this venue."),
        InvalidDeal(var errors) =>
            ErrorDefinition.Validation<InvalidDeal>(
                "The opportunity deal is invalid.",
                errors.ToDictionary())
    };

    [ErrorCode("opportunity.venue_not_found")]
    public partial record VenueNotFound;

    [ErrorCode("opportunity.venue_forbidden")]
    public partial record VenueForbidden;

    [ErrorCode("opportunity.deal.invalid")]
    public partial record InvalidDeal(ValidationErrors Errors);
}
