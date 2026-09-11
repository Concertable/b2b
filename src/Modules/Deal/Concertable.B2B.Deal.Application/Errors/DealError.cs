using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Deal.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record DealError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var dealId) =>
            ErrorDefinition.NotFound<NotFound>($"Deal {dealId} was not found.")
    };

    [ErrorCode("deal.get.not_found")]
    public partial record NotFound(int DealId);
}
