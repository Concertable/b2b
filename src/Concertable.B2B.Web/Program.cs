using Concertable.B2B.Web;
using Concertable.B2B.Web.Middleware;
using Concertable.DataAccess.Application;
using Concertable.ServiceDefaults;
using Concertable.Shared.Notification.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.AddB2BWebHost();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.UseDefaultRateLimiting();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDefaultEndpoints();
app.MapControllers();
app.MapHub<NotificationHub>("/hub/notifications");

app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
    if (File.Exists(indexPath))
        await context.Response.SendFileAsync(indexPath);
    else
        context.Response.StatusCode = StatusCodes.Status404NotFound;
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await initializer.InitializeAsync();
}

app.Run();

public sealed partial class Program
{ }
