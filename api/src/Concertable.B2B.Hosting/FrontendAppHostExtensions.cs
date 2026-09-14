using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Concertable.Frontend.Hosting;
using Microsoft.Extensions.Configuration;

namespace Concertable.B2B.Hosting.Frontend;

public static class FrontendAppHostExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<NodeAppResource> AddVenueSpa(
            IResourceBuilder<IResourceWithServiceDiscovery> backend,
            IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
            builder.AddSpaSurface(
                B2BLocalSpaSurfaces.Venue,
                [["app", "web", "b2b", "venue"], ["app", "web", "venue"]],
                backend,
                auth);

        public IResourceBuilder<NodeAppResource> AddArtistSpa(
            IResourceBuilder<IResourceWithServiceDiscovery> backend,
            IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
            builder.AddSpaSurface(
                B2BLocalSpaSurfaces.Artist,
                [["app", "web", "b2b", "artist"], ["app", "web", "artist"]],
                backend,
                auth);

        public IResourceBuilder<NodeAppResource> AddBusinessSpa(
            IResourceBuilder<IResourceWithServiceDiscovery> backend,
            IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
            builder.AddSpaSurface(
                B2BLocalSpaSurfaces.Business,
                [["app", "web", "b2b", "business"], ["app", "web", "business"]],
                backend,
                auth);

        public IResourceBuilder<NodeAppResource> AddAdminSpa(
            IResourceBuilder<IResourceWithServiceDiscovery> backend,
            IResourceBuilder<IResourceWithServiceDiscovery> auth) =>
            builder.AddSpaSurface(B2BLocalSpaSurfaces.Admin, [["app", "web", "admin"]], backend, auth);

        public IResourceBuilder<NodeAppResource> AddB2BMobileSurface(
            IResourceBuilder<IResourceWithServiceDiscovery> api,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb,
            IResourceBuilder<DevTunnelResource> tunnel) =>
            builder.AddMobileSurface(
                "mobile-b2b",
                [["app", "mobile", "b2b"], ["app", "mobile"]],
                api,
                tunnel,
                new(api, "EXPO_PUBLIC_API_URL"),
                new(auth, "EXPO_PUBLIC_AUTH_AUTHORITY"),
                new(paymentWeb, "EXPO_PUBLIC_PAYMENT_API_URL"));
        public IResourceBuilder<DevTunnelResource>? AddMobileB2B(
            IResourceBuilder<IResourceWithServiceDiscovery> api,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
        {
            if (!builder.Configuration.GetValue<bool>("RunMobile"))
                return null;

            var tunnel = builder.AddMobileTunnel("b2b-dev", auth, api, paymentWeb);
            builder.AddB2BMobileSurface(api, auth, paymentWeb, tunnel);
            return tunnel;
        }
    }
}
