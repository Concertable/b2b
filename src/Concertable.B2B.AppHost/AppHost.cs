using Concertable.B2B.Hosting.Frontend;
using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Payment.Hosting;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:d888cb59d806241611eb36a5d1eeb293d472cd94e4a89bf6dab6b9b23c0c5f82";
    private const string PaymentWebImage = "ghcr.io/concertable/payment-web";
    private const string PaymentWebDigest = "sha256:24503f1c17bce5a67b4544455ee3886e76c6711f1eed57b5c6c74d2c45febdbd";
    private const string PaymentWorkersImage = "ghcr.io/concertable/payment-workers";
    private const string PaymentWorkersDigest = "sha256:5b31c52965054dce47c1cd8a1d1feabd976222f31be28bd8d185c281d014e52f";

    public static IDistributedApplicationBuilder CreateBuilder(string[] args) =>
        CreateBuilder<Projects.Concertable_B2B_Web>(args);

    /// <summary>
    /// The same graph with the B2B web host substituted. The E2E suite passes
    /// <c>Concertable.B2B.E2ETests.Web</c>, which wraps this host and adds the <c>/_e2e</c> admin
    /// surface its fixture resets and seeds through; the production host does not carry it and
    /// must not.
    /// </summary>
    public static IDistributedApplicationBuilder CreateBuilder<TWebProject>(string[] args)
        where TWebProject : IProjectMetadata, new()
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var sql = builder.AddSqlServerContainer("concertable-b2b-sql-data");
        var b2bDb = sql.AddDatabase(B2BConstants.Database);
        var authDb = sql.AddDatabase(AuthConstants.Database);
        var paymentDb = sql.AddDatabase(PaymentConstants.Database);
        var (storage, blobs) = builder.AddAzureStorage();
        var asb = builder.AddServiceBus();
        asb.Topology().AddB2BTopology().AddSearchTopology().AddPaymentTopology().AddAuthTopology().RunAsEmulator();
        var auth = builder.AddAuth(AuthImage, AuthDigest, authDb, asb)
                          .WithContainerRuntimeArgs("--user", "root")
                          .WithHttpsEndpoint(targetPort: AuthConstants.ContainerPort, name: "https");
        auth.WithSpaClients(B2BLocalSpaSurfaces.AuthClients);
        var paymentWeb = builder.AddPaymentWeb(PaymentWebImage, PaymentWebDigest, auth, paymentDb, asb);
        var api = builder.AddB2BWeb<TWebProject>(b2bDb, auth, storage, blobs, asb, paymentWeb);
        auth.WithEnvironment("Services__B2BApiUrl", api.GetEndpoint("https"));
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        var workers = builder.AddB2BWorkers<Projects.Concertable_B2B_Workers>(b2bDb, paymentWeb, auth);
        if (builder.ExecutionContext.IsRunMode)
        {
            api.WithEnvironment(PaymentConstants.AllowInsecureHttpClientEnvironmentVariable, bool.TrueString);
            workers.WithEnvironment(PaymentConstants.AllowInsecureHttpClientEnvironmentVariable, bool.TrueString);
        }
        builder.AddPaymentWorkers(PaymentWorkersImage, PaymentWorkersDigest, paymentDb, asb);
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
