using Concertable.B2B.Application.Domain.State;
using Dunet;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record AcceptApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Ineligible(var error) => error.Definition,
        InvalidState(var state) => ErrorDefinition.Conflict<InvalidState>(
            $"Cannot accept an application from {state}."),
        TermsChanged => ErrorDefinition.Conflict<TermsChanged>(
            "The deal terms have changed since the artist applied. The artist must re-apply before acceptance."),
        PaymentMethodRequired => ErrorDefinition.Invalid<PaymentMethodRequired>(
            "This deal requires a payment method at acceptance."),
        UnsupportedDeal(var dealType) => ErrorDefinition.Invalid<UnsupportedDeal>(
            $"Deal {dealType} does not support acceptance.")
    };

    public partial record Ineligible(ApplicationEligibilityError Error);

    [ErrorCode("application.accept.invalid_state")]
    public partial record InvalidState(ApplicationState State);

    [ErrorCode("application.accept.terms_changed")]
    public partial record TermsChanged;

    [ErrorCode("application.accept.payment_method_required")]
    public partial record PaymentMethodRequired;

    [ErrorCode("application.accept.unsupported_deal")]
    public partial record UnsupportedDeal(DealType DealType);
}
