using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Frontend.Hosting;
using Concertable.Payment.Hosting;
using Concertable.Search.Hosting;

public static class B2BAppHost
{
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
        var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, b2bDb, asb);
        var paymentWeb = builder.AddPaymentWeb<Projects.Concertable_Payment_Web>(auth, paymentDb, asb);
        var api = builder.AddB2BWeb<Projects.Concertable_B2B_Web>(b2bDb, auth, storage, blobs, asb, paymentWeb);
        auth.WithEnvironment("Services__B2BApiUrl", api.GetEndpoint("https"));
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        builder.AddB2BWorkers<Projects.Concertable_B2B_Workers>(b2bDb, paymentWeb, auth);
        builder.AddPaymentWorkers<Projects.Concertable_Payment_Workers>(paymentDb, asb);
        builder.AddVenueSpa(api, auth);
        builder.AddArtistSpa(api, auth);
        builder.AddBusinessSpa(api, auth);
        builder.AddAdminSpa(api, auth);
        builder.AddMobileB2B(api, auth, paymentWeb);
        builder.AddStripeCli(paymentWeb);
        return builder;
    }
}
