using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Mocks;
using Concertable.Payment.Application.Interfaces;
using Concertable.Payment.Application.Interfaces.Webhook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concertable.Shared.Geocoding.Application;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class TestClientOptions
{
    public Action<IConfigurationBuilder>? Configure { get; set; }
    public Action<IServiceCollection>? Services { get; set; }

    public TestClientOptions UseFailingStripe()
    {
        Services += services => services.Replace(ServiceDescriptor.Singleton<IWebhookSimulator, MockWebhookSimulatorFail>());
        return this;
    }

    public TestClientOptions UseFailingPayment()
    {
        Services += services => services.Replace(ServiceDescriptor.Singleton<IStripeApiClient, MockStripeApiClientFail>());
        return this;
    }

    public TestClientOptions UseDeclineAtVerify()
    {
        Services += services =>
        {
            services.Replace(ServiceDescriptor.Scoped<IStripeAccountClient, MockStripeAccountClientFail>());
            services.Replace(ServiceDescriptor.Scoped<IStripeHoldClient, MockStripeHoldClientFail>());
        };
        return this;
    }

    public TestClientOptions UseFailingGeocoding()
    {
        Services += services => services.Replace(ServiceDescriptor.Scoped<IGeocodingClient, MockGeocodingClientFail>());
        return this;
    }
}
