namespace Concertable.B2B.DataAccess.Application;

public interface IConcurrencyVersioned
{
    uint Version { get; }
}
