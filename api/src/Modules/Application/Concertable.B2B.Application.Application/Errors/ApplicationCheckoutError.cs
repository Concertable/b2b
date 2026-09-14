using Concertable.Payment.Contracts.Errors;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ApplicationCheckoutError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Ineligible(var error) => error.Definition,
        ApplicationNotFound => ErrorDefinition.NotFound<ApplicationNotFound>(
            "Concert application does not exist"),
        OpportunityNotFound => ErrorDefinition.NotFound<OpportunityNotFound>(
            "Concert opportunity does not exist"),
        DealNotFound => ErrorDefinition.NotFound<DealNotFound>(
            "Deal does not exist"),
        ArtistNotFound => ErrorDefinition.NotFound<ArtistNotFound>(
            "Artist does not exist"),
        VenueNotFound => ErrorDefinition.NotFound<VenueNotFound>(
            "Venue does not exist"),
        MissingTenant => ErrorDefinition.Forbidden<MissingTenant>(
            "No active organization was found for the current user."),
        ApplyCheckoutUnsupported(var dealType) => ErrorDefinition.Invalid<ApplyCheckoutUnsupported>(
            $"Deal {dealType} does not support a pre-apply checkout."),
        AcceptCheckoutUnsupported(var dealType) => ErrorDefinition.Invalid<AcceptCheckoutUnsupported>(
            $"Deal {dealType} does not support an accept checkout."),
        PaymentSessionUnavailable(var error) => error.Definition
    };

    public partial record Ineligible(ApplicationEligibilityError Error);

    [ErrorCode("application.checkout.application_not_found")]
    public partial record ApplicationNotFound;

    [ErrorCode("application.checkout.opportunity_not_found")]
    public partial record OpportunityNotFound;

    [ErrorCode("application.checkout.deal_not_found")]
    public partial record DealNotFound;

    [ErrorCode("application.checkout.artist_not_found")]
    public partial record ArtistNotFound;

    [ErrorCode("application.checkout.venue_not_found")]
    public partial record VenueNotFound;

    [ErrorCode("application.checkout.missing_tenant")]
    public partial record MissingTenant;

    [ErrorCode("application.checkout.apply_unsupported")]
    public partial record ApplyCheckoutUnsupported(DealType DealType);

    [ErrorCode("application.checkout.accept_unsupported")]
    public partial record AcceptCheckoutUnsupported(DealType DealType);

    public partial record PaymentSessionUnavailable(PaymentOperationError Error);
}
