using Concertable.B2B.Venue.Contracts;
using Concertable.B2B.Venue.Domain.Entities;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Repositories;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Venue.UnitTests;

public sealed class DbContextStanceTests
{
    [Fact]
    public async Task Contexts_TenancyStances_EnforceCapabilitiesAndFilters()
    {
        var provider = new VenueConfigurationProvider();
        await using var readContext = new VenueDbContext(
            CreateOptions<VenueDbContext>(),
            provider);
        await using var tenantContext = new TenantVenueDbContext(
            CreateOptions<TenantVenueDbContext>(),
            provider,
            Mock.Of<ITenantContext>());

        Assert.IsAssignableFrom<IReadDbContext>(readContext);
        Assert.False(typeof(IDbContext).IsAssignableFrom(readContext.GetType()));
        Assert.Equal(QueryTrackingBehavior.NoTracking, readContext.ChangeTracker.QueryTrackingBehavior);
        Assert.Empty(readContext.Model.FindEntityType(typeof(VenueEntity))!.GetDeclaredQueryFilters());
        await Assert.ThrowsAsync<InvalidOperationException>(() => readContext.SaveChangesAsync());
        Assert.IsAssignableFrom<IDbContext>(tenantContext);
        Assert.NotEmpty(tenantContext.Model.FindEntityType(typeof(VenueEntity))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(tenantContext.Model.FindEntityType(typeof(VenueImageEntity))!.GetDeclaredQueryFilters());
    }

    [Fact]
    public async Task OrgIdentityLookup_ReadsAcrossTenants()
    {
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var provider = new VenueConfigurationProvider();
        var tenantId = Guid.NewGuid();

        await using (var tenantContext = new TenantVenueDbContext(
                         CreateInMemoryOptions<TenantVenueDbContext>(databaseName, databaseRoot),
                         provider,
                         Mock.Of<ITenantContext>(t => t.IsHost == true)))
        {
            var venue = VenueEntity.Create(
                Guid.NewGuid(),
                "Venue name",
                "About",
                "banner",
                "avatar",
                new Point(0, 0),
                new Address("County", "Town"),
                "venue@example.com");
            venue.TenantId = tenantId;
            tenantContext.Venues.Add(venue);
            await tenantContext.SaveChangesAsync();
        }

        await using var readContext = new VenueDbContext(
            CreateInMemoryOptions<VenueDbContext>(databaseName, databaseRoot),
            provider);
        var lookup = new VenueOrgIdentityLookup(readContext);

        Assert.Equal(
            new VenueOrgIdentity("Venue name", "County", "Town"),
            await lookup.GetByTenantIdAsync(tenantId));
        Assert.Null(await lookup.GetByTenantIdAsync(Guid.NewGuid()));
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(
                "Server=localhost;Database=ContextStanceTests;User Id=sa;Password=Password123!;TrustServerCertificate=True",
                sql => sql.UseNetTopologySuite())
            .Options;

    private static DbContextOptions<TContext> CreateInMemoryOptions<TContext>(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;
}
