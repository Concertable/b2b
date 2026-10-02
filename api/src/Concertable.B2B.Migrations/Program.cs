using Concertable.B2B.DataAccess.Infrastructure;

var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{B2BDb.Name}")
    ?? throw new InvalidOperationException(
        $"Connection string 'ConnectionStrings__{B2BDb.Name}' is required for the B2B migration job.");

await B2BMigrationJob.RunAsync(connectionString).ConfigureAwait(false);
