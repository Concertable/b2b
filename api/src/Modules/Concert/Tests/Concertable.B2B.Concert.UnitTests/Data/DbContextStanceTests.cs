using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class DbContextStanceTests
{
    private static readonly Type[] TenantFilteredTypes =
    [
        typeof(ConcertEntity),
        typeof(InvoiceEntity),
        typeof(SelfBillingAgreementEntity)
    ];

    [Fact]
    public async Task Contexts_TenancyStances_EnforceCapabilitiesAndFilters()
    {
        var provider = new ConcertConfigurationProvider();
        await using var readContext = new ConcertReadDbContext(
            CreateOptions<ConcertReadDbContext>(),
            provider);
        await using var tenantContext = new ConcertDbContext(
            CreateOptions<ConcertDbContext>(),
            Options.Create(new OutboxOptions()),
            provider,
            Mock.Of<ITenantContext>(),
            DesignTimeResourceAccessContext.Instance);

        Assert.IsAssignableFrom<IReadDbContext>(readContext);
        Assert.False(typeof(IDbContext).IsAssignableFrom(readContext.GetType()));
        Assert.Equal(QueryTrackingBehavior.NoTracking, readContext.ChangeTracker.QueryTrackingBehavior);
        Assert.All(TenantFilteredTypes, type =>
            Assert.Empty(readContext.Model.FindEntityType(type)!.GetDeclaredQueryFilters()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => readContext.SaveChangesAsync());
        Assert.IsAssignableFrom<IDbContext>(tenantContext);
        Assert.All(TenantFilteredTypes, type =>
            Assert.NotEmpty(tenantContext.Model.FindEntityType(type)!.GetDeclaredQueryFilters()));
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(
                "Host=localhost;Database=ContextStanceTests;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;
}
