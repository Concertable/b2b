using Concertable.B2B.Web;
var builder = WebApplication.CreateBuilder(args);
builder.AddB2BWebHost();

var app = builder.Build();
await app.UseB2BWebHost();

app.Run();

public sealed partial class Program
{ }
