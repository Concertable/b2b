using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.Kernel;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record AcceptApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Ineligible(var error) => error.Definition,
        InvalidTransition(var error) => ErrorDefinition.Conflict<InvalidTransition>(
            $"Cannot accept an application from {error.Current}."),
        TermsChanged => ErrorDefinition.Conflict<TermsChanged>(
            "The deal terms have changed since the artist applied. The artist must re-apply before acceptance."),
        AlreadyAccepted => ErrorDefinition.Conflict<AlreadyAccepted>(
            "Another application for this opportunity has already been accepted."),
        Superseded(var applicationId) => ErrorDefinition.Conflict<Superseded>(
            $"Application {applicationId} changed while this acceptance was in flight."),
        PaymentMethodRequired => ErrorDefinition.Invalid<PaymentMethodRequired>(
            "This deal requires a payment method at acceptance.")
    };

    public partial record Ineligible(ApplicationEligibilityError Error);

    [ErrorCode("application.accept.invalid_state")]
    public partial record InvalidTransition(TransitionError<ApplicationState, ApplicationTrigger> Error);

    [ErrorCode("application.accept.terms_changed")]
    public partial record TermsChanged;

    [ErrorCode("application.accept.duplicate")]
    public partial record AlreadyAccepted;

    [ErrorCode("application.accept.superseded")]
    public partial record Superseded(int ApplicationId);

    [ErrorCode("application.accept.payment_method_required")]
    public partial record PaymentMethodRequired;
}
