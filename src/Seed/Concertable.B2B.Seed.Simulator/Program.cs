using Concertable.B2B.Seed.Simulator;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSeedSimulatorHost();
var app = builder.Build();
app.Run();
