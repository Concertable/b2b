using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.User.Infrastructure.Data.Configurations;

internal sealed class PasswordResetTokenEntityConfiguration : IEntityTypeConfiguration<PasswordResetTokenEntity>
{
    public void Configure(EntityTypeBuilder<PasswordResetTokenEntity> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Token).IsUnique();
    }
}
