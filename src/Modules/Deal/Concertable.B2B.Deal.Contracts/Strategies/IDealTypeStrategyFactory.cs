namespace Concertable.B2B.Deal.Contracts;

public interface IDealTypeStrategyFactory<TStrategy>
    where TStrategy : class
{
    TStrategy Create(DealType dealType);
}