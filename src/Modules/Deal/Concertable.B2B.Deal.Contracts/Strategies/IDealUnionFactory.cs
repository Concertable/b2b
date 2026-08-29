namespace Concertable.B2B.Deal.Contracts;

public interface IDealUnionFactory<TUnion>
{
    TUnion Create(DealType dealType);
}
