using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Concertable.Messaging.AzureServiceBus.Options;
using Microsoft.Extensions.Configuration;

namespace Concertable.B2B.Hosting;

public static class AppHostExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ServiceContainerResource> AddB2BWeb(
            string image,
            string digest,
            IResourceBuilder<SqlServerDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<AzureStorageResource> storage,
            IResourceBuilder<AzureBlobStorageResource> blobs,
            IResourceBuilder<AzureServiceBusResource> asb,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
        {
            var b2bSecret = builder.Configuration["ServiceAuth:B2BClientSecret"];
            return builder.AddContainerImage(B2BConstants.WebResource, image, digest)
                          .WithReference(sql)
                          .WaitFor(sql)
                          .WithReference(auth)
                          .WaitFor(auth)
                          .WithReference(blobs)
                          .WaitFor(storage)
                          .WithReference(asb)
                          .WaitFor(asb)
                          .WithReference(paymentWeb)
                          .WaitFor(paymentWeb)
                          .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                          .WithSpaCorsOrigins(B2BLocalSpaSurfaces.All)
                          .WithEnvironment(AzureServiceBusOptions.ServiceNameEnvVar, B2BConstants.ServiceName)
                          .WithEnvironment("ServiceAuth__ClientId", "concertable-b2b")
                          .WithOptionalEnvironment("ServiceAuth__ClientSecret", b2bSecret);
        }

        public IResourceBuilder<ProjectResource> AddB2BWeb<TProject>(
            IResourceBuilder<SqlServerDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<AzureStorageResource> storage,
            IResourceBuilder<AzureBlobStorageResource> blobs,
            IResourceBuilder<AzureServiceBusResource> asb,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
            where TProject : IProjectMetadata, new()
        {
            var b2bSecret = builder.Configuration["ServiceAuth:B2BClientSecret"];
            return builder.AddProject<TProject>(B2BConstants.WebResource)
                          .WithReference(sql)
                          .WaitFor(sql)
                          .WithReference(auth)
                          .WaitFor(auth)
                          .WithReference(blobs)
                          .WaitFor(storage)
                          .WithReference(asb)
                          .WaitFor(asb)
                          .WithReference(paymentWeb)
                          .WaitFor(paymentWeb)
                          .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                          .WithSpaCorsOrigins(B2BLocalSpaSurfaces.All)
                          .WithEnvironment(AzureServiceBusOptions.ServiceNameEnvVar, B2BConstants.ServiceName)
                          .WithEnvironment("ServiceAuth__ClientId", "concertable-b2b")
                          .WithOptionalEnvironment("ServiceAuth__ClientSecret", b2bSecret);
        }

        public IResourceBuilder<AzureFunctionsProjectResource> AddB2BWorkers<TProject>(
            IResourceBuilder<SqlServerDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null,
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null)
            where TProject : IProjectMetadata, new()
        {
            var workers = builder.AddAzureFunctionsProject<TProject>(B2BConstants.WorkersResource)
                                 .WithReference(sql)
                                 .WaitFor(sql);

            if (paymentWeb is not null)
                workers = workers.WithReference(paymentWeb).WaitFor(paymentWeb);

            if (auth is not null)
                workers = workers.WithReference(auth)
                                 .WaitFor(auth)
                                 .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                                 .WithEnvironment("ServiceAuth__ClientId", "concertable-b2b")
                                 .WithOptionalEnvironment("ServiceAuth__ClientSecret", builder.Configuration["ServiceAuth:B2BClientSecret"]);

            return workers;
        }

        public IResourceBuilder<ProjectResource> AddB2BSeedingSimulator<TProject>(
            IResourceBuilder<AzureServiceBusResource> asb)
            where TProject : IProjectMetadata, new()
        {
            return builder.AddProject<TProject>(B2BConstants.SeedingSimulatorResource)
                          .WithReference(asb)
                          .WaitFor(asb);
        }

        public IResourceBuilder<ServiceContainerResource> AddB2BWorkers(
            string image,
            string digest,
            IResourceBuilder<SqlServerDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null,
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null)
        {
            var workers = builder.AddContainerImage(B2BConstants.WorkersResource, image, digest)
                                 .WithReference(sql)
                                 .WaitFor(sql);

            if (paymentWeb is not null)
                workers = workers.WithReference(paymentWeb).WaitFor(paymentWeb);

            if (auth is not null)
                workers = workers.WithReference(auth)
                                 .WaitFor(auth)
                                 .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                                 .WithEnvironment("ServiceAuth__ClientId", "concertable-b2b")
                                 .WithOptionalEnvironment("ServiceAuth__ClientSecret", builder.Configuration["ServiceAuth:B2BClientSecret"]);

            return workers;
        }

        public IResourceBuilder<ServiceContainerResource> AddB2BSeedingSimulator(
            string image,
            string digest,
            IResourceBuilder<AzureServiceBusResource> asb)
        {
            return builder.AddContainerImage(B2BConstants.SeedingSimulatorResource, image, digest)
                          .WithReference(asb)
                          .WaitFor(asb);
        }
    }

    extension<T>(IResourceBuilder<T> resource)
        where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithLocalSpaCorsOrigins(
            IReadOnlyList<LocalSpaSurface> surfaces)
        {
            for (var index = 0; index < surfaces.Count; index++)
                resource = resource.WithEnvironment($"Cors__AllowedOrigins__{index}", surfaces[index].Origin);

            return resource;
        }

        public IResourceBuilder<T> WithSpaCorsOrigins(
            IReadOnlyList<SpaSurface> surfaces)
        {
            for (var index = 0; index < surfaces.Count; index++)
                resource = resource.WithEnvironment($"Cors__AllowedOrigins__{index}", surfaces[index].Origin);

            return resource;
        }
    }
}
