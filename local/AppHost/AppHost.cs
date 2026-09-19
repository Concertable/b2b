using Concertable.B2B.Hosting.Frontend;
using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Payment.Hosting;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:53d75c608f3d51afc52ef3e1462be6ef79809634b196832bae45cf9ac132bbd4";
    private const string PaymentWebImage = "ghcr.io/concertable/payment-web";
    private const string PaymentWebDigest = "sha256:2c7a9b30291d4adb9d94dcf0d047b86809ad487d1c7848ffc340b226497d578d";
    private const string PaymentWorkersImage = "ghcr.io/concertable/payment-workers";
    private const string PaymentWorkersDigest = "sha256:09b1d4d0f9bf175f61dcaafde0d06b7eb7f8a710e0e775d4b385957b9e865cfc";

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
        var sql = builder.AddSqlServerContainer("concertable-b2b-sql-data");
        var b2bDb = sql.AddDatabase(B2BConstants.Database);
        var authDb = sql.AddDatabase(AuthConstants.Database);
        var paymentDb = sql.AddDatabase(PaymentConstants.Database);
        var (storage, blobs) = builder.AddAzureStorage();
        var asb = builder.AddServiceBus();
        asb.Topology().AddB2BTopology().AddSearchTopology().AddPaymentTopology().AddAuthTopology().RunAsEmulator();
        var auth = builder.AddAuth(AuthImage, AuthDigest, authDb, asb)
                          .WithContainerRuntimeArgs("--user", "root")
                          .WithHttpEndpoint(targetPort: AuthConstants.ContainerPort, name: "https");
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
