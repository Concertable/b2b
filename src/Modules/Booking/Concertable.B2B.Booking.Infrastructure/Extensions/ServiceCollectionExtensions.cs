using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Infrastructure.Events;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Repositories;
using Concertable.B2B.Booking.Infrastructure.Services;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBookingModule(IConfiguration configuration)
        {
            services.AddDbContext<BookingDbContext>((provider, options) =>
                options.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                    .AddInterceptors(
                        provider.GetRequiredService<AuditInterceptor>(),
                        provider.GetRequiredService<TenantInterceptor>(),
                        provider.GetRequiredService<IDomainEventDispatchInterceptor>()));

            services.AddScoped<IUnitOfWork<BookingDbContext>, UnitOfWork<BookingDbContext>>();
            services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
            services.AddScoped<IOutboxUnitOfWorkBehavior, OutboxUnitOfWorkBehavior>();
            services.AddScoped(typeof(IStepResolver<>), typeof(StepResolver<>));
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IContractPdfRenderer, ContractPdfRenderer>();
            services.AddScoped<IBookingModule, BookingModule>();
            services.AddKeyedScoped<IConfirmStep, FlatFeeConfirmStep>(DealType.FlatFee);
            services.AddKeyedScoped<IConfirmStep, DoorSplitConfirmStep>(DealType.DoorSplit);
            services.AddKeyedScoped<IConfirmStep, VersusConfirmStep>(DealType.Versus);
            services.AddKeyedScoped<IConfirmStep, VenueHireConfirmStep>(DealType.VenueHire);
            services.AddScoped<IPreCommitDomainEventHandler<ApplicationAcceptedDomainEvent>,
                ApplicationAcceptedDomainEventHandler>();
            services.AddScoped<IPreCommitDomainEventHandler<VerifyPaymentSucceeded>,
                VerifyPaymentSucceededHandler>();
            services.AddScoped<IPreCommitDomainEventHandler<VerifyPaymentFailed>,
                VerifyPaymentFailedHandler>();

            services.AddSingleton<BookingConfigurationProvider>();
            services.AddSingleton<IEntityTypeConfigurationProvider>(provider =>
                provider.GetRequiredService<BookingConfigurationProvider>());

            return services;
        }
    }
}
