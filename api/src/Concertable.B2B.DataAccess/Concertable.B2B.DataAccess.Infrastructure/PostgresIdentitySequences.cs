using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class PostgresIdentitySequences
{
    public static async Task SynchronizeAsync(
        string connectionString,
        IReadOnlyCollection<string> schemas,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var catalogCommand = connection.CreateCommand();
        var schemaParameters = schemas.Select((_, index) => $"@schema{index}").ToArray();
        catalogCommand.CommandText = $"""
            SELECT table_schema, table_name, column_name,
                   pg_get_serial_sequence(format('%I.%I', table_schema, table_name), column_name)
            FROM information_schema.columns
            WHERE is_identity = 'YES'
              AND table_schema IN ({string.Join(", ", schemaParameters)})
            """;
        for (var index = 0; index < schemas.Count; index++)
            catalogCommand.Parameters.AddWithValue(schemaParameters[index], schemas.ElementAt(index));

        List<(string Schema, string Table, string Column, string Sequence)> identities = [];
        await using (var reader = await catalogCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                identities.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }

        var commandBuilder = new NpgsqlCommandBuilder();
        foreach (var identity in identities)
        {
            var schema = commandBuilder.QuoteIdentifier(identity.Schema);
            var table = commandBuilder.QuoteIdentifier(identity.Table);
            var column = commandBuilder.QuoteIdentifier(identity.Column);
            await using var synchronizeCommand = connection.CreateCommand();
            synchronizeCommand.CommandText = $"""
                SELECT setval(@sequence::regclass, COALESCE(MAX({column}), 1), COUNT(*) > 0)
                FROM {schema}.{table}
                """;
            synchronizeCommand.Parameters.AddWithValue("sequence", identity.Sequence);
            await synchronizeCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
