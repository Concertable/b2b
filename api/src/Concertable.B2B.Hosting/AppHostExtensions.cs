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
        public IResourceBuilder<ServiceContainerResource> AddB2BMigrations(
            string image,
            string digest,
            IResourceBuilder<PostgresDatabaseResource> database) =>
            builder.AddContainerImage(B2BMigrations.Name, image, digest)
                .WithReference(database)
                .WaitFor(database);

        public IResourceBuilder<ProjectResource> AddB2BMigrations<TProject>(
            IResourceBuilder<PostgresDatabaseResource> database)
            where TProject : IProjectMetadata, new() =>
            builder.AddProject<TProject>(B2BMigrations.Name)
                .WithReference(database)
                .WaitFor(database);

        public IResourceBuilder<ServiceContainerResource> AddB2BWeb(
            string image,
            string digest,
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<AzureStorageResource> storage,
            IResourceBuilder<AzureBlobStorageResource> blobs,
            IResourceBuilder<AzureServiceBusResource> asb,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
        {
            var b2bSecret = builder.Configuration["ServiceAuth:B2BClientSecret"];
            return builder.AddContainerImage(B2BWeb.Name, image, digest)
                          .WithHttpEndpoint(targetPort: B2BWeb.ContainerPort, name: "https")
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
                          .WithEnvironment(AzureServiceBusOptions.ServiceNameEnvVar, B2BService.Name)
                          .WithEnvironment("ServiceAuth__ClientId", B2BService.Name)
                          .WithOptionalEnvironment("ServiceAuth__ClientSecret", b2bSecret);
        }

        public IResourceBuilder<ProjectResource> AddB2BWeb<TProject>(
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<AzureStorageResource> storage,
            IResourceBuilder<AzureBlobStorageResource> blobs,
            IResourceBuilder<AzureServiceBusResource> asb,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
            where TProject : IProjectMetadata, new()
        {
            var b2bSecret = builder.Configuration["ServiceAuth:B2BClientSecret"];
            return builder.AddProject<TProject>(B2BWeb.Name)
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
                          .WithEnvironment(AzureServiceBusOptions.ServiceNameEnvVar, B2BService.Name)
                          .WithEnvironment("ServiceAuth__ClientId", B2BService.Name)
                          .WithOptionalEnvironment("ServiceAuth__ClientSecret", b2bSecret);
        }

        public IResourceBuilder<AzureFunctionsProjectResource> AddB2BWorkers<TProject>(
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null,
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null)
            where TProject : IProjectMetadata, new()
        {
            var workers = builder.AddAzureFunctionsProject<TProject>(B2BWorkers.Name)
                                 .WithReference(sql)
                                 .WaitFor(sql);

            if (paymentWeb is not null)
                workers = workers.WithReference(paymentWeb).WaitFor(paymentWeb);

            if (auth is not null)
                workers = workers.WithReference(auth)
                                 .WaitFor(auth)
                                 .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                                 .WithEnvironment("ServiceAuth__ClientId", B2BService.Name)
                                 .WithOptionalEnvironment("ServiceAuth__ClientSecret", builder.Configuration["ServiceAuth:B2BClientSecret"]);

            return workers;
        }

        public IResourceBuilder<ProjectResource> AddB2BSeedingSimulator<TProject>(
            IResourceBuilder<AzureServiceBusResource> asb)
            where TProject : IProjectMetadata, new()
        {
            return builder.AddProject<TProject>(B2BSeedingSimulator.Name)
                          .WithReference(asb)
                          .WaitFor(asb);
        }

        public IResourceBuilder<ServiceContainerResource> AddB2BWorkers(
            string image,
            string digest,
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null,
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null)
        {
            var workers = builder.AddContainerImage(B2BWorkers.Name, image, digest)
                                 .WithReference(sql)
                                 .WaitFor(sql);

            if (paymentWeb is not null)
                workers = workers.WithReference(paymentWeb).WaitFor(paymentWeb);

            if (auth is not null)
                workers = workers.WithReference(auth)
                                 .WaitFor(auth)
                                 .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"))
                                 .WithEnvironment("ServiceAuth__ClientId", B2BService.Name)
                                 .WithOptionalEnvironment("ServiceAuth__ClientSecret", builder.Configuration["ServiceAuth:B2BClientSecret"]);

            return workers;
        }

        public IResourceBuilder<ServiceContainerResource> AddB2BSeedingSimulator(
            string image,
            string digest,
            IResourceBuilder<AzureServiceBusResource> asb)
        {
            return builder.AddContainerImage(B2BSeedingSimulator.Name, image, digest)
                          .WithReference(asb)
                          .WaitFor(asb);
        }
    }

    extension<T>(IResourceBuilder<T> resource)
        where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithSpaCorsOrigins(
            IReadOnlyList<SpaSurface> surfaces)
        {
            for (var index = 0; index < surfaces.Count; index++)
                resource = resource.WithEnvironment($"Cors__AllowedOrigins__{index}", surfaces[index].Origin);

            return resource;
        }
    }
}
