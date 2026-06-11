using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.B2B.Tenant.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Contract.Infrastructure.Extensions;
using Concertable.B2B.Venue.Infrastructure.Extensions;
using Concertable.Shared.Blob.Infrastructure.Extensions;
using Concertable.Shared.Email.Infrastructure.Extensions;
using Concertable.Shared.Geocoding.Infrastructure.Extensions;
using Concertable.Shared.Imaging.Infrastructure.Extensions;
using Concertable.Shared.Pdf.Infrastructure.Extensions;
using Concertable.B2B.User.Infrastructure.Extensions;
using Concertable.B2B.Conversations.Infrastructure.Extensions;
using Concertable.Messaging.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Concertable.Shared.Notification.Infrastructure.Extensions;
using Concertable.Payment.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Extensions;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Seed.Shared.Extensions;

namespace Concertable.B2B.Workers;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSeedingInfrastructure();
        services.AddSharedInfrastructure(configuration);
        services.AddSharedBlob(configuration);
        services.AddSharedEmail(configuration);
        services.AddSharedGeocoding();
        services.AddSharedImaging();
        services.AddSharedPdf();
        services.AddInMemoryTransport();
        services.AddDirectBusKeyed("webhook");
        services.AddOutbox(
            opt => opt.UseSqlServer(configuration.GetConnectionString(B2BDb.Name)),
            runDispatcher: false);
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TenantInterceptor>();
        services.AddScoped<IDomainEventDispatchInterceptor, DomainEventDispatchInterceptor>();

        services.AddReadDbContext(configuration);

        services.AddGeometry();

        services.AddCurrentUser();
        services.AddTenantModule(configuration);
        services.AddUserModule(configuration);
        services.AddArtistModule(configuration);
        services.AddVenueModule(configuration);
        services.AddConcertModule(configuration);
        services.AddContractModule(configuration);
        services.AddClientCredentials(opts =>
        {
            opts.Authority = configuration["Auth:Authority"] ?? configuration["services:auth:https:0"] ?? "";
            opts.ClientId = configuration["ServiceAuth:ClientId"] ?? "";
            opts.ClientSecret = configuration["ServiceAuth:ClientSecret"] ?? "";
        });
        services.AddPaymentClient(configuration);
        services.AddNotificationClient();
        services.AddConversationsModule(configuration);

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
