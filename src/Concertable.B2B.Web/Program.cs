using Concertable.Kernel.Serializers;
using Concertable.B2B.Web;
using Concertable.B2B.Artist.Api.Extensions;
using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.B2B.Venue.Api.Extensions;
using Concertable.B2B.Venue.Infrastructure.Extensions;
using Concertable.B2B.Concert.Api.Extensions;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Contract.Api.Extensions;
using Concertable.B2B.Contract.Infrastructure.Extensions;
using Concertable.Payment.Client.Extensions;
using Concertable.Payment.Domain.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Conversations.Infrastructure.Extensions;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.B2B.Organization.Api.Extensions;
using Concertable.B2B.User.Api.Extensions;
using Concertable.B2B.User.Infrastructure.Extensions;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Shared.Blob.Infrastructure.Extensions;
using Concertable.Shared.Email.Infrastructure.Extensions;
using Concertable.Shared.Geocoding.Infrastructure.Extensions;
using Concertable.Shared.Imaging.Infrastructure.Extensions;
using Concertable.Shared.Pdf.Infrastructure.Extensions;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Seed;
using Concertable.Seed.Events;
using Concertable.Seed.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Seed.Contracts;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concertable.Payment.Seed;
using Concertable.B2B.Web.Extensions;
using Concertable.Shared.Notification.Infrastructure.Hubs;
using Concertable.Shared.Notification.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Concertable.ServiceDefaults;
using Concertable.DataAccess.Application;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.Kernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureBlobClient("blobs");

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Concertable.Shared.Api.Controllers.GenreController).Assembly)
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
        reg.Publishes<ArtistChangedEvent>();
        reg.Publishes<ArtistRatingUpdatedEvent>();
        reg.Publishes<VenueChangedEvent>();
        reg.Publishes<VenueRatingUpdatedEvent>();
        reg.Publishes<ConcertChangedEvent>();
        reg.Publishes<ConcertPostedEvent>();
        reg.Publishes<ConcertRatingUpdatedEvent>();

        reg.SubscribeTo<CredentialRegisteredEvent>();
        reg.SubscribeTo<CustomerReviewSubmittedEvent>();
        reg.SubscribeTo<PaymentSucceededEvent>();
        reg.SubscribeTo<PaymentFailedEvent>();
    });
services.AddDirectBusKeyed("webhook");
services.AddOutbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("B2BDb")));
services.AddInbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("B2BDb")));
services.AddInProcessEventDispatch();
services.AddSeedingInfrastructure();
if (!builder.Environment.IsEnvironment("Testing"))
{
    services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatchInterceptor, SeedingDomainEventDispatchInterceptor>());
    services.AddScoped<IDbInitializer, DevDbInitializer>();
    services.AddSingleton<SeedCatalog>();
    services.AddScoped<SeedData>();
    services.AddBlobDevSeeder();
    services.AddUserDevSeeder();
    services.AddArtistDevSeeder();
    services.AddVenueDevSeeder();
    services.AddContractDevSeeder();
    services.AddConcertDevSeeder();
    services.AddConversationsDevSeeder();
}
services.AddServices(builder.Configuration);
services.AddRepositories();
services.AddNotificationClient();
services.AddOrganizationApi(builder.Configuration);
services.AddConversationsApi(builder.Configuration);
services.AddArtistApi(builder.Configuration);
services.AddVenueApi(builder.Configuration);
services.AddConcertApi(builder.Configuration);
services.AddContractApi(builder.Configuration);
services.AddPaymentClient(builder.Configuration);
services.AddQueueHostedService();
services.AddCurrentUser();
services.AddUserApi(builder.Configuration);
services.AddAuth(builder.Configuration, builder.Environment);
services.AddValidation();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

if (builder.Environment.IsEnvironment("E2E"))
    services.AddSingleton<StripeE2EAccountResolver>();

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
