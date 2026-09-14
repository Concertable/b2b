namespace Concertable.B2B.Deal.Contracts;

public interface IDealStrategyFactory<TStrategy>
    where TStrategy : class, IDealStrategy
{
    TStrategy Create(DealType dealType);
}
