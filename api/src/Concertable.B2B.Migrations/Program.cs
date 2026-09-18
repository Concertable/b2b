var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb")
    ?? throw new InvalidOperationException(
        "Connection string 'ConnectionStrings__B2BDb' is required for the B2B migration job.");

await B2BMigrationJob.RunAsync(connectionString).ConfigureAwait(false);
