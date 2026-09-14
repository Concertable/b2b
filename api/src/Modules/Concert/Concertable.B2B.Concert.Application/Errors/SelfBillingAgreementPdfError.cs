using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record SelfBillingAgreementPdfError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound =>
            ErrorDefinition.NotFound<NotFound>("Self-Billing Agreement not found")
    };

    [ErrorCode("self_billing.pdf.not_found")]
    public partial record NotFound;
}
