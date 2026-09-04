using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Frontend.Hosting;
using Concertable.Payment.Hosting;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:8b7ba47efb319e6e1f1b5b86223d4075b9c8e09920933dae24fbf35f72851a63";
    private const string PaymentWebImage = "ghcr.io/concertable/payment-web";
    private const string PaymentWebDigest = "sha256:11f02cfa129cf82709dbceb438a281c7fa66b8594643a32a3d599142958fd696";
    private const string PaymentWorkersImage = "ghcr.io/concertable/payment-workers";
    private const string PaymentWorkersDigest = "sha256:dc9670dffdd9b8f63cbae682c9c81be2b52f0fad394aa239debed632009d144c";

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
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
        var paymentWeb = builder.AddPaymentWeb(PaymentWebImage, PaymentWebDigest, auth, paymentDb, asb)
                                .WithHttpEndpoint(targetPort: 8080, name: "https")
                                .WithHttpEndpoint(targetPort: 8080, name: "http");
        var api = builder.AddB2BWeb<Projects.Concertable_B2B_Web>(b2bDb, auth, storage, blobs, asb, paymentWeb);
        auth.WithEnvironment("Services__B2BApiUrl", api.GetEndpoint("https"));
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        builder.AddB2BWorkers<Projects.Concertable_B2B_Workers>(b2bDb, paymentWeb, auth);
        builder.AddPaymentWorkers(PaymentWorkersImage, PaymentWorkersDigest, paymentDb, asb);
        builder.AddVenueSpa(api, auth);
        builder.AddArtistSpa(api, auth);
        builder.AddBusinessSpa(api, auth);
        builder.AddAdminSpa(api, auth);
        builder.AddMobileB2B(api, auth, paymentWeb);
        builder.AddStripeCli(paymentWeb);
        return builder;
    }
}
