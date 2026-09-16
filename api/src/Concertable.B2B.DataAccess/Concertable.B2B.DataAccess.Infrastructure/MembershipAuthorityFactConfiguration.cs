using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// Maps the Tenant module's membership rows read-only into a resource module's context, so an access
/// predicate can re-check the caller's membership revision in the same statement that reads the resource.
/// Tenant owns and migrates the table; this mapping is excluded from migrations and never written through.
/// </summary>
internal sealed class MembershipAuthorityFactConfiguration : IEntityTypeConfiguration<MembershipAuthorityFact>
{
    private const string TenantSchema = "tenant";
    private const string MembershipsTable = "Memberships";

    public void Configure(EntityTypeBuilder<MembershipAuthorityFact> builder)
    {
        builder.ToTable(MembershipsTable, TenantSchema, table => table.ExcludeFromMigrations());
        builder.HasKey(m => new { m.TenantId, m.UserId });
        builder.Property(m => m.TenantId).ValueGeneratedNever();
        builder.Property(m => m.UserId).ValueGeneratedNever();
        builder.Property(m => m.AuthorizationVersion).ValueGeneratedNever();
    }
}
