using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The columns and indexes every grant family shares. A module's own configuration derives from this and adds
/// the real foreign key to its resource — that key is module-local, so only the module can declare it.
/// </summary>
public abstract class ResourceAccessGrantConfiguration<TGrant, TScope> : IEntityTypeConfiguration<TGrant>
    where TGrant : ResourceAccessGrant<TScope>
    where TScope : struct, Enum
{
    protected abstract string TableName { get; }
    protected abstract string SchemaName { get; }

    public void Configure(EntityTypeBuilder<TGrant> builder)
    {
        builder.ToTable(TableName, SchemaName);
        builder.HasKey(g => g.Id);
        builder.Property(g => g.ResourceId).IsRequired();
        builder.Property(g => g.TenantId).IsRequired();
        builder.Property(g => g.Scope).IsRequired();
        builder.Property(g => g.ValidFrom).IsRequired();
        builder.Property(g => g.IssuedByTenantId).IsRequired();
        builder.Property(g => g.Kind).IsRequired();
        builder.Property(g => g.Version).IsRequired().IsConcurrencyToken();

        /* Both directions are read constantly: the resource-first index serves "may this membership reach this
           row", the tenant-first one serves "what may this tenant reach". Expiry stays out of the index
           condition — a moving predicate would make the index depend on the clock. */
        builder.HasIndex(g => new { g.ResourceId, g.TenantId, g.Scope, g.MembershipId })
            .HasFilter($"[{nameof(ResourceAccessGrant<TScope>.RevokedAt)}] IS NULL");
        builder.HasIndex(g => new { g.TenantId, g.Scope, g.ResourceId, g.MembershipId })
            .HasFilter($"[{nameof(ResourceAccessGrant<TScope>.RevokedAt)}] IS NULL");

        /* Two rules rather than one over a nullable column: SQL Server treats NULLs as equal in a unique
           index, so a single index would let one member-specific grant block every other member's. */
        builder.HasIndex(g => new { g.ResourceId, g.TenantId, g.Scope, g.Kind, g.IssuedByTenantId })
            .IsUnique()
            .HasFilter(
                $"[{nameof(ResourceAccessGrant<TScope>.RevokedAt)}] IS NULL AND " +
                $"[{nameof(ResourceAccessGrant<TScope>.MembershipId)}] IS NULL")
            .HasDatabaseName($"UX_{TableName}_Tenant");
        builder.HasIndex(g => new { g.ResourceId, g.TenantId, g.Scope, g.Kind, g.IssuedByTenantId, g.MembershipId })
            .IsUnique()
            .HasFilter(
                $"[{nameof(ResourceAccessGrant<TScope>.RevokedAt)}] IS NULL AND " +
                $"[{nameof(ResourceAccessGrant<TScope>.MembershipId)}] IS NOT NULL")
            .HasDatabaseName($"UX_{TableName}_Membership");
    }
}
