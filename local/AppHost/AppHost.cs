using Concertable.B2B.Hosting.Frontend;
using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Payment.Hosting;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:cbd7c429da9d9dd2cc674177760690c53d1414e8057e368eefc3631dfcb62be6";
    private const string AuthMigrationsImage = "ghcr.io/concertable/auth-migrations";
    private const string AuthMigrationsDigest = "sha256:090b1bb80dc7b708508a03883cdfb8e8805b36918589e6d14f2f350cc61c5dcb";
    private const string PaymentWebImage = "ghcr.io/concertable/payment-web";
    private const string PaymentWebDigest = "sha256:13bc1a58a647e01618822935985097e3c667b3ddb365def899ba83cf5d5b574f";
    private const string PaymentWorkersImage = "ghcr.io/concertable/payment-workers";
    private const string PaymentWorkersDigest = "sha256:586f87d0866bcf325622793379f15f4b94abdd34091907ca94b4e15fa5b2ab64";
    private const string PaymentMigrationsImage = "ghcr.io/concertable/payment-migrations";
    private const string PaymentMigrationsDigest = "sha256:b22e1a0d498e01d12b49e0bc911f3e4c7e44a0d5e3c4d5c8917e854e1fccdc96";

    public static IDistributedApplicationBuilder CreateBuilder(string[] args) =>
        ConfigureBuilder<Projects.Concertable_B2B_Web>(StrictDistributedApplication.CreateBuilder(args));

    public static IDistributedApplicationBuilder CreateE2EBuilder<TWebProject>()
        where TWebProject : IProjectMetadata, new() =>
        ConfigureBuilder<TWebProject>(DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--environment", "Development"],
            AssemblyName = typeof(AppHost).Assembly.GetName().Name!,
            DisableDashboard = true,
        }));

    private static IDistributedApplicationBuilder ConfigureBuilder<TWebProject>(IDistributedApplicationBuilder builder)
        where TWebProject : IProjectMetadata, new()
    {
        var postgres = builder.AddPostgresContainer("concertable-b2b-postgres-data")
            .WithPostGis()
            .WithArgs("-c", "max_prepared_transactions=100");
        var b2bDb = postgres.AddDatabase(B2BDatabase.Name);
        var authDb = postgres.AddDatabase(AuthConstants.Database);
        var paymentDb = postgres.AddDatabase(PaymentConstants.Database);
        var (storage, blobs) = builder.AddAzureStorage();
        var asb = builder.AddServiceBus();
        asb.Topology().AddB2BTopology().AddSearchTopology().AddPaymentTopology().AddAuthTopology().RunAsEmulator();
        var authMigrations = builder.AddAuthMigrations(AuthMigrationsImage, AuthMigrationsDigest, authDb);
        // Duende writes its developer signing key to /app/tempkey.jwk at startup, which the image's own
        // non-root user cannot write to. AddAuth's image overload declares no endpoint and binds plaintext
        // on its container port, but consumers resolve it through GetEndpoint("https"), so the endpoint
        // carrying that traffic must be named "https" while staying HTTP.
        var auth = builder.AddAuth(AuthImage, AuthDigest, authDb, authMigrations, asb)
                          .WithContainerRuntimeArgs("--user", "root")
                          .WithHttpEndpoint(targetPort: AuthConstants.ContainerPort, name: "https");
        auth.WithSpaClients(B2BLocalSpaSurfaces.AuthClients);
        var paymentMigrations = builder.AddPaymentMigrations(
            PaymentMigrationsImage, PaymentMigrationsDigest, paymentDb);
        var paymentWeb = builder.AddPaymentWeb(PaymentWebImage, PaymentWebDigest, auth, paymentDb, asb)
            .WaitForCompletion(paymentMigrations);
        var migrations = builder.AddB2BMigrations<Projects.Concertable_B2B_Migrations>(b2bDb);
        var api = builder.AddB2BWeb<TWebProject>(b2bDb, auth, storage, blobs, asb, paymentWeb)
            .WaitForCompletion(migrations);
        auth.WithEnvironment("Services__B2BApiUrl", api.GetEndpoint("https"));
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        var workers = builder.AddB2BWorkers<Projects.Concertable_B2B_Workers>(b2bDb, paymentWeb, auth)
            .WaitForCompletion(migrations);
        if (builder.ExecutionContext.IsRunMode)
        {
            api.WithEnvironment(PaymentConstants.AllowInsecureHttpClientEnvironmentVariable, bool.TrueString);
            workers.WithEnvironment(PaymentConstants.AllowInsecureHttpClientEnvironmentVariable, bool.TrueString);
        }
        builder.AddPaymentWorkers(PaymentWorkersImage, PaymentWorkersDigest, paymentDb, asb)
            .WaitForCompletion(paymentMigrations);
        builder.AddVenueSpa(api, auth);
        builder.AddArtistSpa(api, auth);
        builder.AddBusinessSpa(api, auth);
        builder.AddAdminSpa(api, auth);
        if (builder.AddMobileB2B(api, auth, paymentWeb) is { } mobileTunnel)
            auth.WithMobilePublicUrl(mobileTunnel.GetEndpoint(auth, "https"));
        builder.AddStripeCli(paymentWeb);
        return builder;
    }
}
