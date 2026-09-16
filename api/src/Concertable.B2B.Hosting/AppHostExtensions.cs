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
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb) =>
            WebImage(builder, image, digest, sql, auth, storage, blobs, asb, paymentWeb);

        public IResourceBuilder<ServiceContainerResource> AddB2BWeb(
            string image,
            string digest,
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<AzureStorageResource> storage,
            IResourceBuilder<AzureBlobStorageResource> blobs,
            IResourceBuilder<AzureServiceBusResource> asb,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb) =>
            WebImage(builder, image, digest, sql, auth, storage, blobs, asb, paymentWeb);

        public IResourceBuilder<ProjectResource> AddB2BWeb<TProject>(
            IResourceBuilder<SqlServerDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<AzureStorageResource> storage,
            IResourceBuilder<AzureBlobStorageResource> blobs,
            IResourceBuilder<AzureServiceBusResource> asb,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
            where TProject : IProjectMetadata, new() =>
            WebProject<TProject>(builder, sql, auth, storage, blobs, asb, paymentWeb);

        public IResourceBuilder<ProjectResource> AddB2BWeb<TProject>(
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<AzureStorageResource> storage,
            IResourceBuilder<AzureBlobStorageResource> blobs,
            IResourceBuilder<AzureServiceBusResource> asb,
            IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
            where TProject : IProjectMetadata, new() =>
            WebProject<TProject>(builder, sql, auth, storage, blobs, asb, paymentWeb);

        public IResourceBuilder<AzureFunctionsProjectResource> AddB2BWorkers<TProject>(
            IResourceBuilder<SqlServerDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null,
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null)
            where TProject : IProjectMetadata, new() =>
            WorkersProject<TProject>(builder, sql, paymentWeb, auth);

        public IResourceBuilder<AzureFunctionsProjectResource> AddB2BWorkers<TProject>(
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null,
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null)
            where TProject : IProjectMetadata, new() =>
            WorkersProject<TProject>(builder, sql, paymentWeb, auth);

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
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null) =>
            WorkersImage(builder, image, digest, sql, paymentWeb, auth);

        public IResourceBuilder<ServiceContainerResource> AddB2BWorkers(
            string image,
            string digest,
            IResourceBuilder<PostgresDatabaseResource> sql,
            IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb = null,
            IResourceBuilder<IResourceWithServiceDiscovery>? auth = null) =>
            WorkersImage(builder, image, digest, sql, paymentWeb, auth);

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
        public IResourceBuilder<T> WithSpaCorsOrigins(
            IReadOnlyList<SpaSurface> surfaces)
        {
            for (var index = 0; index < surfaces.Count; index++)
                resource = resource.WithEnvironment($"Cors__AllowedOrigins__{index}", surfaces[index].Origin);

            return resource;
        }
    }

    private static IResourceBuilder<ServiceContainerResource> WebImage(
        IDistributedApplicationBuilder builder,
        string image,
        string digest,
        IResourceBuilder<IResourceWithConnectionString> sql,
        IResourceBuilder<IResourceWithServiceDiscovery> auth,
        IResourceBuilder<AzureStorageResource> storage,
        IResourceBuilder<AzureBlobStorageResource> blobs,
        IResourceBuilder<AzureServiceBusResource> asb,
        IResourceBuilder<IResourceWithServiceDiscovery> paymentWeb)
    {
        var b2bSecret = builder.Configuration["ServiceAuth:B2BClientSecret"];
        return builder.AddContainerImage(B2BConstants.WebResource, image, digest)
                      .WithHttpEndpoint(targetPort: B2BConstants.ContainerPort, name: "https")
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

    private static IResourceBuilder<ProjectResource> WebProject<TProject>(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> sql,
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

    private static IResourceBuilder<AzureFunctionsProjectResource> WorkersProject<TProject>(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> sql,
        IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb,
        IResourceBuilder<IResourceWithServiceDiscovery>? auth)
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

    private static IResourceBuilder<ServiceContainerResource> WorkersImage(
        IDistributedApplicationBuilder builder,
        string image,
        string digest,
        IResourceBuilder<IResourceWithConnectionString> sql,
        IResourceBuilder<IResourceWithServiceDiscovery>? paymentWeb,
        IResourceBuilder<IResourceWithServiceDiscovery>? auth)
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
}
