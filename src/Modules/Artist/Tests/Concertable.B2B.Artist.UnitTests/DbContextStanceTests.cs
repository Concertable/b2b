using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Artist.Domain.Entities;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Repositories;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Artist.UnitTests;

public sealed class DbContextStanceTests
{
    [Fact]
    public async Task Contexts_TenancyStances_EnforceCapabilitiesAndFilters()
    {
        var provider = new ArtistConfigurationProvider();
        await using var readContext = new ArtistDbContext(
            CreateOptions<ArtistDbContext>(),
            provider);
        await using var tenantContext = new TenantArtistDbContext(
            CreateOptions<TenantArtistDbContext>(),
            provider,
            Mock.Of<ITenantContext>());

        Assert.IsAssignableFrom<IReadDbContext>(readContext);
        Assert.False(typeof(IDbContext).IsAssignableFrom(readContext.GetType()));
        Assert.Equal(QueryTrackingBehavior.NoTracking, readContext.ChangeTracker.QueryTrackingBehavior);
        Assert.Empty(readContext.Model.FindEntityType(typeof(ArtistEntity))!.GetDeclaredQueryFilters());
        await Assert.ThrowsAsync<InvalidOperationException>(() => readContext.SaveChangesAsync());
        Assert.IsAssignableFrom<IDbContext>(tenantContext);
        Assert.NotEmpty(tenantContext.Model.FindEntityType(typeof(ArtistEntity))!.GetDeclaredQueryFilters());
    }

    [Fact]
    public async Task OrgIdentityLookup_ReadsAcrossTenants()
    {
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var provider = new ArtistConfigurationProvider();
        var tenantId = Guid.NewGuid();

        await using (var tenantContext = new TenantArtistDbContext(
                         CreateInMemoryOptions<TenantArtistDbContext>(databaseName, databaseRoot),
                         provider,
                         Mock.Of<ITenantContext>(t => t.IsHost == true)))
        {
            var artist = ArtistEntity.Create(
                Guid.NewGuid(),
                "Artist name",
                "About",
                "banner",
                "avatar",
                new Point(0, 0),
                new Address("County", "Town"),
                "artist@example.com",
                []);
            artist.TenantId = tenantId;
            tenantContext.Artists.Add(artist);
            await tenantContext.SaveChangesAsync();
        }

        await using var readContext = new ArtistDbContext(
            CreateInMemoryOptions<ArtistDbContext>(databaseName, databaseRoot),
            provider);
        var lookup = new ArtistOrgIdentityLookup(readContext);

        Assert.Equal(
            new ArtistOrgIdentity("Artist name", "County", "Town"),
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
