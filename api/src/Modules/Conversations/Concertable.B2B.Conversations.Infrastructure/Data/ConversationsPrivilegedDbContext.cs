using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

/// <summary>
/// The Conversations module's platform-admin stance: the same anemic configuration as
/// <see cref="ConversationsDbContext"/>, writable, with no tenant filter — a platform operator moderates
/// threads they are not party to. The tenant-filtered counterpart is <see cref="ConversationsDbContext"/>.
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
