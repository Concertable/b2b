using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Data;

public sealed class DbContextStanceTests
{
    private static readonly Type[] TenantFilteredTypes =
    [
        typeof(ApplicationEntity),
        typeof(BookingEntity),
        typeof(ContractEntity),
        typeof(InvoiceEntity),
        typeof(SelfBillingAgreementEntity)
    ];

    [Fact]
    public async Task Contexts_TenancyStances_EnforceCapabilitiesAndFilters()
    {
        var provider = new ConcertConfigurationProvider();
        await using var readContext = new ConcertDbContext(
            CreateOptions<ConcertDbContext>(),
            provider);
        await using var tenantContext = new ConcertTenantDbContext(
            CreateOptions<ConcertTenantDbContext>(),
            provider,
            Mock.Of<ITenantContext>());

        Assert.IsAssignableFrom<IReadDbContext>(readContext);
        Assert.False(typeof(IDbContext).IsAssignableFrom(readContext.GetType()));
        Assert.Equal(QueryTrackingBehavior.NoTracking, readContext.ChangeTracker.QueryTrackingBehavior);
        Assert.All(TenantFilteredTypes, type =>
            Assert.Empty(readContext.Model.FindEntityType(type)!.GetDeclaredQueryFilters()));
        Assert.Empty(tenantContext.Model.FindEntityType(typeof(ConcertEntity))!.GetDeclaredQueryFilters());
        await Assert.ThrowsAsync<InvalidOperationException>(() => readContext.SaveChangesAsync());
        Assert.IsAssignableFrom<IDbContext>(tenantContext);
        Assert.All(TenantFilteredTypes, type =>
            Assert.NotEmpty(tenantContext.Model.FindEntityType(type)!.GetDeclaredQueryFilters()));
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(
                "Server=localhost;Database=ContextStanceTests;User Id=sa;Password=Password123!;TrustServerCertificate=True",
                sql => sql.UseNetTopologySuite())
            .Options;
}
