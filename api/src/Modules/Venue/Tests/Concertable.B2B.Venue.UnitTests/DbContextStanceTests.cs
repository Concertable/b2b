using Concertable.B2B.Venue.Domain.Entities;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Concertable.B2B.Venue.UnitTests;

public sealed class DbContextStanceTests
{
    [Fact]
    public async Task Contexts_TenancyStances_EnforceCapabilitiesAndFilters()
    {
        var provider = new VenueConfigurationProvider();
        await using var readContext = new VenueReadDbContext(
            CreateOptions<VenueReadDbContext>(),
            provider);
        await using var tenantContext = new VenueDbContext(
            CreateOptions<VenueDbContext>(),
            Options.Create(new OutboxOptions()),
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
            .UseNpgsql(
                "Host=localhost;Database=ContextStanceTests;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;

}
