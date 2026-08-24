using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Admin.Api.Extensions;
using Concertable.B2B.Admin.Infrastructure.Extensions;
using Concertable.B2B.Application.Api.Extensions;
using Concertable.B2B.Artist.Api.Extensions;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Booking.Api.Extensions;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Api.Extensions;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Conversations.Infrastructure.Extensions;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Deal.Api.Extensions;
using Concertable.B2B.Opportunity.Api.Extensions;
using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Tenant.Api.Extensions;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Extensions;
using Concertable.B2B.User.Api.Extensions;
using Concertable.B2B.User.Infrastructure.Extensions;
using Concertable.B2B.Venue.Api.Extensions;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.B2B.Web.Extensions;
using Concertable.B2B.Web.Middleware;
using Concertable.B2B.Web.Routing;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel;
using Concertable.Kernel.Extensions;
using Concertable.Kernel.Serializers;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.Payment.Client.Extensions;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Events;
using Concertable.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.ServiceDefaults;
using Concertable.Shared.Api.Exceptions;
using Concertable.Shared.Api.Extensions;
using Concertable.Shared.Blob.Infrastructure.Extensions;
using Concertable.Shared.Email.Application;
using Concertable.Shared.Email.Infrastructure.Extensions;
using Concertable.Shared.Geocoding.Infrastructure.Extensions;
using Concertable.Shared.Imaging.Infrastructure.Extensions;
using Concertable.Shared.Notification.Infrastructure.Extensions;
using Concertable.Shared.Pdf.Infrastructure.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Concertable.B2B.Web;

public static class B2BWebHostExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddB2BWebHost()
        {
            builder.AddServiceDefaults();
            builder.AddAzureBlobServiceClient("blobs");
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddProblemDetails();
            builder.Services.AddControllers(options =>
                    options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseRouteTransformer())))
                .AddApplicationPart(typeof(Concertable.Shared.Api.Controllers.GenreController).Assembly)
                .AddApplicationJson(options =>
                {
                    options.IncludeFields = true;
                    options.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.WriteIndented = true;
                    options.Converters.Add(new TimeOnlyJsonConverter());
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
                    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins(corsOrigins);
                });
            });

            var services = builder.Services;
            services.AddScoped<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);
            services.AddInfrastructure(builder.Configuration);
            services.AddClientCredentials(opts =>
            {
                opts.Authority = builder.Configuration["Auth:Authority"] ?? builder.Configuration["services__auth__https__0"]
                    ?? (builder.Environment.IsIntegration() ? null!
                        : throw new InvalidOperationException("Auth:Authority is required (no explicit key and no service-discovery fallback)."));
                opts.ClientId = builder.Configuration["ServiceAuth:ClientId"]
                    ?? (builder.Environment.IsIntegration() ? null!
                        : throw new InvalidOperationException("ServiceAuth:ClientId is required."));
                if (builder.Configuration["ServiceAuth:ClientSecret"] is string clientSecret)
                    opts.ClientSecret = clientSecret;
            });
            services.AddSharedBlob(builder.Configuration);
            services.AddSharedEmail(builder.Configuration);
            services.AddSharedGeocoding();
            services.AddSharedImaging();
            services.AddSharedPdf();
            services.AddAzureServiceBusTransport(
                opts =>
                {
                    opts.ConnectionString = builder.Configuration.GetConnectionString("asb")
                        ?? (builder.Environment.IsIntegration() ? null!
                            : throw new InvalidOperationException("Connection string 'asb' is required."));
                    opts.ServiceName = builder.Configuration["ServiceBus:ServiceName"]
                        ?? (builder.Environment.IsIntegration() ? "concertable-b2b"
                            : throw new InvalidOperationException("Configuration 'ServiceBus:ServiceName' is required."));
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
                    reg.Publishes<BookingCancelledEvent>();
                    reg.Publishes<ConcertCancelledEvent>();
                    reg.Publishes<ConcertCreatedEvent>();
                    reg.Publishes<Concertable.B2B.Tenant.Contracts.Events.PayoutOwnerRegisteredEvent>();
                    reg.Publishes<Concertable.B2B.Tenant.Contracts.Events.TenantActivityRecordedEvent>();
                    reg.SendsTo<CaptureEscrowCommand>(PaymentServiceIdentity.Name);
                    reg.SendsTo<DepositEscrowCommand>(PaymentServiceIdentity.Name);
                    reg.SendsTo<RefundEscrowCommand>(PaymentServiceIdentity.Name);
                    reg.SubscribeTo<CredentialRegisteredEvent>();
                    reg.SubscribeTo<CustomerReviewSubmittedEvent>();
                    reg.SubscribeTo<PaymentSucceededEvent>();
                    reg.SubscribeTo<PaymentFailedEvent>();
                    reg.SubscribeTo<CaptureEscrowSucceededEvent>();
                    reg.SubscribeTo<CaptureEscrowRejectedEvent>();
                    reg.SubscribeTo<DepositEscrowSucceededEvent>();
                    reg.SubscribeTo<DepositEscrowRejectedEvent>();
                    reg.SubscribeTo<RefundEscrowSucceededEvent>();
                    reg.SubscribeTo<RefundEscrowRejectedEvent>();
                    reg.SubscribeTo<BookingCancelledEvent>();
                    reg.SubscribeTo<ConcertCancelledEvent>();
                    reg.SubscribeTo<ConcertCreatedEvent>();
                    reg.HandleCommand<SendEmailCommand>();
                });
            services.AddDirectBusKeyed("webhook");
            services.AddOutbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString(B2BDb.Name)));
            services.AddInbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString(B2BDb.Name)));
            services.AddInProcessEventDispatch();
            services.AddSeedingInfrastructure();
            if (!builder.Environment.IsIntegration())
            {
                services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatchInterceptor, SeedingDomainEventDispatchInterceptor>());
                services.AddScoped<IDbInitializer, DevDbInitializer>();
                services.AddSingleton<SeedCatalog>();
                services.AddScoped<SeedState>();
                services.AddBlobDevSeeder();
                services.AddUserDevSeeder();
                services.AddTenantDevSeeder();
                services.AddAdminDevSeeder();
                services.AddArtistDevSeeder();
                services.AddVenueDevSeeder();
                services.AddDealDevSeeder();
                services.AddConversationsDevSeeder();
            }
            services.AddServices(builder.Configuration);
            services.AddRepositories();
            services.AddNotificationClient();
            services.AddOpportunityApi(builder.Configuration);
            services.AddApplicationApi(builder.Configuration);
            services.AddBookingApi(builder.Configuration);
            services.AddTenantApi(builder.Configuration);
            services.AddConversationsApi(builder.Configuration);
            services.AddArtistApi(builder.Configuration);
            services.AddVenueApi(builder.Configuration);
            services.AddConcertApi(builder.Configuration);
            services.AddDealApi(builder.Configuration);
            if (!builder.Environment.IsIntegration())
                services.AddPaymentClient(builder.Configuration);
            services.AddQueueHostedService();
            services.AddCurrentUser();
            services.AddAdminApi(builder.Configuration);
            services.AddUserApi(builder.Configuration);
            services.AddAuth(builder.Configuration, builder.Environment);
            services.AddValidation();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddScoped<TenantResolutionMiddleware>();
            services.Configure<ForwardedHeadersOptions>(options =>
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
            builder.AddDefaultRateLimiting();
            builder.AddRateLimitPolicy(RateLimitPolicies.PublicRead, new RateLimitWindow { PermitLimit = 100, WindowSeconds = 60 }, perUser: false);
            builder.AddRateLimitPolicy(RateLimitPolicies.Upload, new RateLimitWindow { PermitLimit = 20, WindowSeconds = 60 }, perUser: false);
            builder.AddRateLimitPolicy(RateLimitPolicies.Apply, new RateLimitWindow { PermitLimit = 20, WindowSeconds = 60 }, perUser: true);
            builder.AddRateLimitPolicy(RateLimitPolicies.Messaging, new RateLimitWindow { PermitLimit = 20, WindowSeconds = 60 }, perUser: true);
            builder.AddRateLimitPolicy(RateLimitPolicies.Checkout, new RateLimitWindow { PermitLimit = 10, WindowSeconds = 60 }, perUser: true);
            builder.AddRateLimitPolicy(RateLimitPolicies.ProfileImage, new RateLimitWindow { PermitLimit = 20, WindowSeconds = 60 }, perUser: true);
            return builder;
        }
    }
}
