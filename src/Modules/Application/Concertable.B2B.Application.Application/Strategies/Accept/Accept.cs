using Dunet;

namespace Concertable.B2B.Application.Application.Strategies;

[Union(EnableImplicitConversions = false)]
internal abstract partial record Accept
{
    public partial record Standard(IAccept Accept);

    public partial record Paid(IAcceptPaid Accept);
}
