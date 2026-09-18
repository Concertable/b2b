using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.User.Infrastructure.Data;

internal sealed class UserDbContext(
    DbContextOptions<UserDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    UserConfigurationProvider provider)
    : DbContextBase(options, outboxOptions)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
