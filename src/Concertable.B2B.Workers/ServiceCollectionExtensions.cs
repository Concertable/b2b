using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.B2B.Organization.Infrastructure.Extensions;
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
using Concertable.B2B.Notification.Infrastructure.Extensions;
using Concertable.Payment.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Extensions;
using Concertable.B2B.DataAccess;
using Concertable.Seeding.Extensions;

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
            opt => opt.UseSqlServer(configuration.GetConnectionString("B2BDb")),
            runDispatcher: false);
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddReadDbContext(configuration);

        services.AddGeometry();

        services.AddCurrentUser();
        services.AddOrganizationModule(configuration);
        services.AddUserModule(configuration);
        services.AddArtistModule(configuration);
        services.AddVenueModule(configuration);
        services.AddConcertModule(configuration);
        services.AddContractModule(configuration);
        services.AddClientCredentials(opts =>
        {
            opts.Authority = configuration["Auth:Authority"] ?? configuration["services__auth__https__0"] ?? "";
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
