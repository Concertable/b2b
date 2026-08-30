using Concertable.B2B.E2ETests.Server;
using Concertable.B2B.Web;

namespace Concertable.B2B.E2ETests.Web;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.AddB2BWebHost();
        builder.Services.AddB2BE2EAdmin(builder.Configuration);

        var app = builder.Build();
        app.MapB2BE2EAdmin();
        await app.UseB2BWebHost();
        app.Run();
    }
}
