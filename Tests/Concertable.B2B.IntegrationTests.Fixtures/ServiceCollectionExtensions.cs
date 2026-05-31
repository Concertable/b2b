using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Mocks;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddResettables(
        this IServiceCollection services,
        IMockNotificationClient notificationService,
        MockStripeApiClient stripePaymentClient,
        IMockEmailSender emailSender)
    {
        services.AddSingleton<IResettable>(notificationService);
        services.AddSingleton<IResettable>(stripePaymentClient);
        services.AddSingleton<IResettable>(emailSender);
        return services;
    }
}
