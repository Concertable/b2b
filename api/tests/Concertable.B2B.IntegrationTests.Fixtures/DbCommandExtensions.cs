using System.Data.Common;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class DbCommandExtensions
{
    extension(DbCommand command)
    {
        public bool HasParameter(object value) =>
            command.Parameters.Cast<DbParameter>().Any(parameter => Equals(parameter.Value, value));
    }
}
