using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record OpportunityMutationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        VenueNotFound =>
            ErrorDefinition.For<OpportunityMutationError>().NotFound<VenueNotFound>(
                "No venue was found for the current organization."),
        VenueForbidden =>
            ErrorDefinition.For<OpportunityMutationError>().Forbidden<VenueForbidden>("You do not own this venue."),
        InvalidDeal(var errors) =>
            ErrorDefinition.For<OpportunityMutationError>().Validation<InvalidDeal>(
                "The opportunity deal is invalid.",
                new Reunion.Errors.ValidationErrors(errors.ToDictionary()))
    };

    [ErrorCode("opportunity.venue_not_found")]
    public partial record VenueNotFound;

    [ErrorCode("opportunity.venue_forbidden")]
    public partial record VenueForbidden;

    [ErrorCode("opportunity.deal.invalid")]
    public partial record InvalidDeal(ValidationErrors Errors);
}
