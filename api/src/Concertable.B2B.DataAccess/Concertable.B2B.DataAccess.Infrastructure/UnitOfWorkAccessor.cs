namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class UnitOfWorkAccessor
{
    public UnitOfWork? Current { get; internal set; }
}
