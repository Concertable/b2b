namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class CommandTransactionAccessor
{
    public CommandTransaction? Current { get; internal set; }
}
