using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.User.Infrastructure.Data.Configurations;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable(Schema.Tables.Users, Schema.Name);
        builder.Property(u => u.Location).HasColumnType("geography");
        builder.HasIndex(u => u.Email).IsUnique();
        builder.OwnsAddress(u => u.Address, required: false);
    }
}
