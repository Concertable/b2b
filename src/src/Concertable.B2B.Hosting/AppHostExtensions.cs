using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Concertable.Messaging.AzureServiceBus.Options;
using Microsoft.Extensions.Configuration;

namespace Concertable.B2B.Hosting;

public static class AppHostExtensions
{
    public static IResourceBuilder<ProjectResource> AddB2BWeb<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> sql,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<AzureStorageResource> storage,
        IResourceBuilder<AzureBlobStorageResource> blobs,
        IResourceBuilder<AzureServiceBusResource> asb,
        IResourceBuilder<ProjectResource> paymentWeb)
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
                      .WithLocalSpaCorsOrigins(LocalSpaSurfaces.B2B)
                      .WithEnvironment(AzureServiceBusOptions.ServiceNameEnvVar, B2BConstants.ServiceName)
                      .WithEnvironment("ServiceAuth__ClientId", "concertable-b2b")
                      .WithOptionalEnvironment("ServiceAuth__ClientSecret", b2bSecret);
    }

    public static IResourceBuilder<AzureFunctionsProjectResource> AddB2BWorkers<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> sql,
        IResourceBuilder<ProjectResource>? paymentWeb = null,
        IResourceBuilder<ProjectResource>? auth = null)
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

    public static IResourceBuilder<ProjectResource> AddB2BSeedingSimulator<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>(B2BConstants.SeedingSimulatorResource)
                      .WithReference(asb)
                      .WaitFor(asb);
    }

    extension(IResourceBuilder<ProjectResource> resource)
    {
        public IResourceBuilder<ProjectResource> WithLocalSpaCorsOrigins(
            IReadOnlyList<LocalSpaSurface> surfaces)
        {
            for (var index = 0; index < surfaces.Count; index++)
                resource = resource.WithEnvironment($"Cors__AllowedOrigins__{index}", surfaces[index].Origin);

            return resource;
        }
    }
}
