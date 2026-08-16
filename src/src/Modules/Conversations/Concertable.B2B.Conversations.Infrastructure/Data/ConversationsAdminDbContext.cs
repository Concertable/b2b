using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

/// <summary>
/// The Conversations module's platform-admin stance: the same anemic configuration as
/// <see cref="ConversationsDbContext"/>, writable, with no tenant filter — a platform operator moderates
/// threads they are not party to. The tenant-filtered counterpart is <see cref="ConversationsDbContext"/>.
/// </summary>
internal sealed class ConversationsAdminDbContext(
    DbContextOptions<ConversationsAdminDbContext> options,
    ConversationsConfigurationProvider provider)
    : AdminDbContext(options, provider, Schema.Name)
{
    public DbSet<ContentReportEntity> ContentReports => Set<ContentReportEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
}
