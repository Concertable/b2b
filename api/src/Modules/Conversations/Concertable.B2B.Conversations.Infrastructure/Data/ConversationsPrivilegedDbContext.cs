using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

/// <summary>
/// The Conversations module's platform-admin stance: the same anemic configuration as
/// <see cref="ConversationsDbContext"/>, writable, with neither the tenant filter nor the tenant write
/// guard — a platform operator moderates threads they are not party to, and the guard refuses a write with
/// no current tenant. The tenant-filtered, guarded counterpart is <see cref="ConversationsDbContext"/>.
/// </summary>
internal sealed class ConversationsPrivilegedDbContext(
    DbContextOptions<ConversationsPrivilegedDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ConversationsConfigurationProvider provider)
    : PrivilegedDbContext(options, outboxOptions, provider, Schema.Name)
{
    public DbSet<ContentReportEntity> ContentReports => Set<ContentReportEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
}
