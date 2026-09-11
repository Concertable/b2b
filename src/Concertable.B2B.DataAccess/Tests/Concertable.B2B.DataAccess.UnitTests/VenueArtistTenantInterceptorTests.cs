using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class VenueArtistTenantInterceptorTests
{
    [Fact]
    public async Task SavingChanges_AddedEntityWithoutTenantPair_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        context.Entities.Add(new ScopedEntity());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Contains("without both tenant ids stamped", exception.Message);
    }

    [Fact]
    public void SavingChanges_ChangedPersistedTenantPair_ThrowsInvalidOperationException()
    {
        using var context = CreateContext();
        var entity = new ScopedEntity
        {
            VenueTenantId = Guid.NewGuid(),
            ArtistTenantId = Guid.NewGuid()
        };
        context.Entities.Add(entity);
        context.SaveChanges();
        entity.ArtistTenantId = Guid.NewGuid();

        var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        Assert.Contains("tried to change its venue/artist tenant pair", exception.Message);
    }

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new VenueArtistTenantInterceptor())
            .Options;
        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ScopedEntity> Entities => Set<ScopedEntity>();
    }

    private sealed class ScopedEntity : IVenueArtistTenantScoped
    {
        public int Id { get; private set; }
        public Guid VenueTenantId { get; set; }
        public Guid ArtistTenantId { get; set; }
    }
}
