using Dunet;

namespace Concertable.B2B.Application.Application.Strategies;

[Union(EnableImplicitConversions = false)]
internal abstract partial record Apply
{
    public partial record Standard(IApplyStandard Apply);

    public partial record Prepaid(IApplyPrepaid Apply);
}
