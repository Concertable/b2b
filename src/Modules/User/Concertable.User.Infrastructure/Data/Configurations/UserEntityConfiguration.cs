using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.User.Infrastructure.Data.Configurations;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users", "user");
        builder.Property(u => u.Location).HasColumnType("geography");
        builder.HasIndex(u => new { u.Email, u.Role }).IsUnique();
        builder.OwnsOne(u => u.Address, a =>
        {
            a.Property(x => x.County).HasColumnName("County");
            a.Property(x => x.Town).HasColumnName("Town");
        });
    }
}
