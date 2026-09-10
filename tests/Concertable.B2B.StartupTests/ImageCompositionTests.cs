using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Payment.Hosting;
using Xunit;

namespace Concertable.B2B.StartupTests;

/// <summary>Covers the image overload of B2B's own web service, which no AppHost in this repository
/// composes — every standalone AppHost runs its own service from source and only foreign services by
/// image. That gap is why the overload shipped declaring no endpoint at all while the project overload
/// gets Aspire's defaults for free: a consumer composing B2B by image got a service nothing could
/// reach, and GetEndpoint("https") threw.</summary>
public sealed class ImageCompositionTests
{
    private const string Digest = "sha256:0000000000000000000000000000000000000000000000000000000000000000";

    [Fact]
    public void AddB2BWeb_ByImage_DeclaresTheEndpointConsumersResolve()
    {
        var builder = DistributedApplication.CreateBuilder();
        var sql = builder.AddSqlServer("sql");
        var asb = builder.AddServiceBus();
        var (storage, blobs) = builder.AddAzureStorage();
        var auth = builder.AddContainerImage(AuthConstants.Resource, "ghcr.io/concertable/auth", Digest)
                          .WithHttpsEndpoint(targetPort: AuthConstants.ContainerPort, name: "https");
        var paymentWeb = builder.AddPaymentWeb(
            "ghcr.io/concertable/payment-web",
            Digest,
            auth,
            sql.AddDatabase(PaymentConstants.Database),
            asb);

        var web = builder.AddB2BWeb(
            "ghcr.io/concertable/b2b-web",
            Digest,
            sql.AddDatabase(B2BConstants.Database),
            auth,
            storage,
            blobs,
            asb,
            paymentWeb);

        var endpoint = Assert.Single(
            web.Resource.Annotations.OfType<EndpointAnnotation>(),
            endpoint => endpoint.Name == "https");

        Assert.Equal("http", endpoint.UriScheme);
        Assert.Equal(B2BConstants.ContainerPort, endpoint.TargetPort);
    }
}
