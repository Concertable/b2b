using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class TenantScopedDbContext : DbContextBase, IHasTenantContext
{
    private readonly IEntityTypeConfigurationProvider provider;
    private readonly string defaultSchema;

    public ITenantContext TenantContext { get; }

    protected TenantScopedDbContext(
        DbContextOptions options,
        IOptions<OutboxOptions> outboxOptions,
        IEntityTypeConfigurationProvider provider,
        ITenantContext tenantContext,
        string defaultSchema)
        : base(options, outboxOptions)
    {
        this.provider = provider;
        this.defaultSchema = defaultSchema;
        TenantContext = tenantContext;
    }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(defaultSchema);
        provider.Configure(modelBuilder);
        ConfigureMembershipAuthority(modelBuilder);
        ApplyTenantFilters(modelBuilder);
    }

    protected virtual void ConfigureMembershipAuthority(ModelBuilder modelBuilder) { }

    protected abstract void ApplyTenantFilters(ModelBuilder modelBuilder);
}
