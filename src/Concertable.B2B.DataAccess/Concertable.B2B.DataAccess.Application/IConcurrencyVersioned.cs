namespace Concertable.B2B.DataAccess.Application;

public interface IConcurrencyVersioned
{
    byte[] Version { get; }
}
