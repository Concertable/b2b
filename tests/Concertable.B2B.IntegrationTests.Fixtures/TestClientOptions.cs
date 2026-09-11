using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concertable.Shared.Geocoding.Application;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.Shared.Email.Application;

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

    public TestClientOptions UseFailingGeocoding()
    {
        Services += services => services.Replace(ServiceDescriptor.Scoped<IGeocodingClient, MockGeocodingClientFail>());
        return this;
    }

    public TestClientOptions UseFailingEmailRendering()
    {
        Services += services => services.Replace(
            ServiceDescriptor.Singleton<IEmailRenderer, FailingEmailRenderer>());
        return this;
    }

    private sealed class FailingEmailRenderer : IEmailRenderer
    {
        public RenderedEmail Render(IEmailContent content) =>
            throw new InvalidOperationException("Email rendering failed.");
    }
}
