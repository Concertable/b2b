var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServerContainer("concertable-b2b-sql-data");
var b2bDb = sql.AddDatabase("B2BDb");
var authDb = sql.AddDatabase("AuthDb");
var searchDb = sql.AddDatabase("SearchDb");
var paymentDb = sql.AddDatabase("PaymentDb");

var (storage, blobs) = builder.AddAzureStorage();
var asb = builder.AddServiceBus();

asb.Topology()
   .AddB2BTopology()
   .AddSearchTopology()
   .AddPaymentTopology();

var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, b2bDb, asb);
var paymentWeb = builder.AddPaymentWeb<Projects.Concertable_Payment_Web>(auth, paymentDb, asb);
var api = builder.AddApi<Projects.Concertable_B2B_Web>(b2bDb, auth, storage, blobs, asb, paymentWeb);

auth.WithEnvironment("Services__B2BApiUrl", api.GetEndpoint("https"));
auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");

builder.AddWorkers<Projects.Concertable_B2B_Workers>(b2bDb, paymentWeb);
builder.AddPaymentWorkers<Projects.Concertable_Payment_Workers>(paymentDb, asb);
builder.AddSearchWeb<Projects.Concertable_Search_Web>(auth, searchDb);
builder.AddSearchWorkers<Projects.Concertable_Search_Workers>(searchDb, asb);
builder.AddVenueSpa(api, auth);
builder.AddArtistSpa(api, auth);
builder.AddBusinessSpa(api, auth);
builder.AddMobileB2B(api, auth);
builder.AddStripeCli(api);

builder.Build().Run();
