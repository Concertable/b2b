using Concertable.B2B.Venue.Domain.Entities;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

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

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(
                "Server=localhost;Database=ContextStanceTests;User Id=sa;Password=Password123!;TrustServerCertificate=True",
                sql => sql.UseNetTopologySuite())
            .Options;
}
