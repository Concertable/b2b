using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Payment.Contracts.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record AcceptApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        Ineligible(var error) => error.Definition,
        TransitionFailure(var error) => error.Definition,
        TermsChanged => ErrorDefinition.Conflict<TermsChanged>(
            "The deal terms have changed since the artist applied. The artist must re-apply before acceptance."),
        PaymentMethodRequired => ErrorDefinition.Invalid<PaymentMethodRequired>(
            "This deal requires a payment method at acceptance."),
        UnsupportedDeal(var dealType) => ErrorDefinition.Invalid<UnsupportedDeal>(
            $"Deal {dealType} does not support acceptance."),
        EscrowCaptureFailure(var error) => error.Definition,
        EscrowDepositFailure(var error) => error.Definition
    };

    public partial record Ineligible(ApplicationEligibilityError Error);
    public partial record TransitionFailure(LifecycleTransitionError Error);

    [ErrorCode("application.accept.terms_changed")]
    public partial record TermsChanged;

    [ErrorCode("application.accept.payment_method_required")]
    public partial record PaymentMethodRequired;

    [ErrorCode("application.accept.unsupported_deal")]
    public partial record UnsupportedDeal(DealType DealType);

    public partial record EscrowCaptureFailure(EscrowCaptureError Error);
    public partial record EscrowDepositFailure(EscrowDepositError Error);
}
