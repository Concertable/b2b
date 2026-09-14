using Dunet;

namespace Concertable.B2B.Concert.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record DoorRevenueDeclarationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NegativeRevenue =>
            ErrorDefinition.Invalid<NegativeRevenue>("Door revenue must be zero or greater.")
    };

    public partial record NegativeRevenue;
}
