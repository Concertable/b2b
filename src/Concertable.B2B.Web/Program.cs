using Concertable.DataAccess;
using Concertable.Application.Serializers;
using Concertable.B2B.Web;
using Concertable.Artist.Api.Extensions;
using Concertable.Artist.Infrastructure.Extensions;
using Concertable.Venue.Api.Extensions;
using Concertable.Venue.Infrastructure.Extensions;
using Concertable.Concert.Api.Extensions;
using Concertable.Concert.Infrastructure.Extensions;
using Concertable.Contract.Api.Extensions;
using Concertable.Contract.Infrastructure.Extensions;
using Concertable.Payment.Client.Extensions;
using Concertable.Payment.Domain.Events;
using Concertable.Concert.Contracts.Events;
using Concertable.Artist.Contracts.Events;
using Concertable.Venue.Contracts.Events;
using Concertable.Messaging.Application;
using Concertable.Conversations.Infrastructure.Extensions;
using Concertable.Messaging.AzureServiceBus;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.Customer.Api.Extensions;
using Concertable.Customer.Infrastructure.Extensions;
using Concertable.Authorization.Infrastructure.Extensions;
using Concertable.Organization.Api.Extensions;
using Concertable.User.Api.Extensions;
using Concertable.User.Infrastructure.Extensions;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Shared.Blob.Infrastructure.Extensions;
using Concertable.Shared.Email.Infrastructure.Extensions;
using Concertable.Shared.Geocoding.Infrastructure.Extensions;
using Concertable.Shared.Imaging.Infrastructure.Extensions;
using Concertable.Shared.Pdf.Infrastructure.Extensions;
using Concertable.Shared.Infrastructure.Extensions;
using Concertable.Seeding.Fakers;
using Concertable.B2B.Web.Extensions;
using Concertable.Notification.Infrastructure.Hubs;
using Concertable.Notification.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureBlobClient("blobs");

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.IncludeFields = true;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithOrigins(corsOrigins);
    });
});

var services = builder.Services;

services.AddScoped<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);

services.AddInfrastructure(builder.Configuration);
services.AddClientCredentials(opts =>
{
    opts.Authority = builder.Configuration["Auth:Authority"] ?? builder.Configuration["services__auth__https__0"] ?? "";
    opts.ClientId = builder.Configuration["ServiceAuth:ClientId"] ?? "";
    opts.ClientSecret = builder.Configuration["ServiceAuth:ClientSecret"] ?? "";
});
services.AddSharedBlob(builder.Configuration);
services.AddSharedEmail(builder.Configuration);
services.AddSharedGeocoding();
services.AddSharedImaging();
services.AddSharedPdf();
services.AddAzureServiceBusTransport(
    opts =>
    {
        opts.ConnectionString = builder.Configuration.GetConnectionString("asb") ?? "";
        opts.ServiceName = "concertable-b2b";
    },
    reg =>
    {
        reg.SubscribeTo<ReviewSubmittedEvent>();
        reg.SubscribeTo<PaymentSucceededEvent>();
        reg.SubscribeTo<PaymentFailedEvent>();
    });
services.AddDirectBusKeyed("webhook");
services.AddOutbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("B2BDb")));
services.AddInbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("B2BDb")));
services.AddInProcessEventDispatch();
if (!builder.Environment.IsEnvironment("Testing"))
{
    services.AddScoped<IDbInitializer, DevDbInitializer>();
    services.AddScoped<Concertable.Seeding.SeedData>();
    services.AddScoped<ILocationFaker, LocationFaker>();
    services.AddBlobDevSeeder();
    services.AddUserDevSeeder();
    services.AddArtistDevSeeder();
    services.AddVenueDevSeeder();
    services.AddContractDevSeeder();
    services.AddConcertDevSeeder();
    services.AddConversationsDevSeeder();
    services.AddCustomerDevSeeder();
}
services.AddServices(builder.Configuration);
services.AddRepositories();
services.AddNotificationModule();
services.AddOrganizationApi(builder.Configuration);
services.AddConversationsApi(builder.Configuration);
services.AddArtistApi(builder.Configuration);
services.AddVenueApi(builder.Configuration);
services.AddConcertApi(builder.Configuration);
services.AddContractApi(builder.Configuration);
services.AddPaymentClient(builder.Configuration);
services.AddCustomerApi(builder.Configuration);
services.AddQueueHostedService();
services.AddAuthorizationModule();
services.AddUserApi(builder.Configuration);
services.AddAuth(builder.Configuration, builder.Environment);
services.AddValidation();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDefaultEndpoints();
app.MapControllers();
app.MapHub<NotificationHub>("/hub/notifications");
app.MapGet("/health", () => Results.Ok());

if (app.Environment.IsEnvironment("E2E"))
    app.MapE2EEndpoints();

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

public partial class Program { }
