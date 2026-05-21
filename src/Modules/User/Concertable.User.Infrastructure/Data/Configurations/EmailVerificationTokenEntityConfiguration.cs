using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.User.Infrastructure.Data.Configurations;

internal sealed class EmailVerificationTokenEntityConfiguration : IEntityTypeConfiguration<EmailVerificationTokenEntity>
{
    public void Configure(EntityTypeBuilder<EmailVerificationTokenEntity> builder)
    {
        builder.ToTable("EmailVerificationTokens");
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Token).IsUnique();
    }
}
