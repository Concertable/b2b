using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Application.Validators;
using FluentValidation;
using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.Conversations.Infrastructure.Data.Seeders;
using Concertable.B2B.Conversations.Infrastructure.Repositories;
using Concertable.B2B.Conversations.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;

namespace Concertable.B2B.Conversations.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConversationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConversationsDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantInterceptor>(),
                    sp.GetRequiredService<VenueArtistTenantInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

        services.AddDbContext<AdminConversationsDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>()));

        services.AddSingleton<ConversationsConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ConversationsConfigurationProvider>());

        services.AddValidatorsFromAssemblyContaining<ReportMessageRequestValidator>(includeInternalTypes: true);

        services.Configure<SafetySettings>(configuration.GetSection(SafetySettings.SectionName));

        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IContentReportRepository, ContentReportRepository>();
        services.AddScoped<IAdminMessageRepository, AdminMessageRepository>();
        services.AddScoped<IAdminContentReportRepository, AdminContentReportRepository>();
        services.AddScoped<IConversationsNotifier, ConversationsNotifier>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IContentReportNotifier, ContentReportNotifier>();
        services.AddScoped<IContentReportService, ContentReportService>();
        services.AddScoped<IModerationService, ModerationService>();
        services.AddScoped<IConversationsModule, ConversationsModule>();

        return services;
    }

    public static IServiceCollection AddConversationsDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, ConversationsDevSeeder>();
        return services;
    }

    public static IServiceCollection AddConversationsTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, ConversationsTestSeeder>();
        return services;
    }
}
