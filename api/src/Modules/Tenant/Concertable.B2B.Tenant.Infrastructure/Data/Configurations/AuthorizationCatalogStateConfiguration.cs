using Concertable.B2B.Authorization.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class AuthorizationCatalogStateConfiguration : IEntityTypeConfiguration<AuthorizationCatalogState>
{
    public void Configure(EntityTypeBuilder<AuthorizationCatalogState> builder)
    {
        builder.ToTable(Schema.Tables.AuthorizationCatalogState, Schema.Name);
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Revision).HasMaxLength(64).IsRequired();
        builder.HasData(new { Id = 1, Revision = AuthorizationCatalog.Revision });
    }
}
