using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The columns and indexes every grant family shares. A module's own configuration derives from this and adds
/// the real foreign key to its resource — that key is module-local, so only the module can declare it.
/// </summary>
public abstract class ResourceAccessGrantConfiguration<TGrant, TFacet> : IEntityTypeConfiguration<TGrant>
    where TGrant : ResourceAccessGrant<TFacet>
    where TFacet : struct, Enum
{
    protected abstract string TableName { get; }
    protected abstract string SchemaName { get; }

    public void Configure(EntityTypeBuilder<TGrant> builder)
    {
        builder.ToTable(TableName, SchemaName);
        builder.HasKey(g => g.Id);
        builder.Property(g => g.ResourceId).IsRequired();
        builder.Property(g => g.TenantId).IsRequired();
        builder.Property(g => g.Facet).IsRequired();
        builder.Property(g => g.ValidFrom).IsRequired();
        builder.Property(g => g.IssuedByTenantId).IsRequired();
        builder.Property(g => g.Origin).IsRequired();
        builder.Property(g => g.Version).IsRequired().IsConcurrencyToken();

        /* Both directions are read constantly: the resource-first index serves "may this tenant reach this
           row", the tenant-first one serves "what may this tenant reach". Expiry stays out of the index
           condition — a moving predicate would make the index depend on the clock. */
        builder.HasIndex(g => new { g.ResourceId, g.TenantId, g.Facet, g.MemberUserId })
            .HasFilter($"[{nameof(ResourceAccessGrant<TFacet>.RevokedAt)}] IS NULL");
        builder.HasIndex(g => new { g.TenantId, g.Facet, g.ResourceId, g.MemberUserId })
            .HasFilter($"[{nameof(ResourceAccessGrant<TFacet>.RevokedAt)}] IS NULL");

        /* Two explicit uniqueness rules rather than one over a nullable column: SQL Server treats NULLs as
           equal in a unique index, so a single index would let one member-specific grant block every other
           member's. */
        builder.HasIndex(g => new { g.ResourceId, g.TenantId, g.Facet })
            .IsUnique()
            .HasFilter(
                $"[{nameof(ResourceAccessGrant<TFacet>.RevokedAt)}] IS NULL AND " +
                $"[{nameof(ResourceAccessGrant<TFacet>.MemberUserId)}] IS NULL")
            .HasDatabaseName($"UX_{TableName}_TenantWide");
        builder.HasIndex(g => new { g.ResourceId, g.TenantId, g.Facet, g.MemberUserId })
            .IsUnique()
            .HasFilter(
                $"[{nameof(ResourceAccessGrant<TFacet>.RevokedAt)}] IS NULL AND " +
                $"[{nameof(ResourceAccessGrant<TFacet>.MemberUserId)}] IS NOT NULL")
            .HasDatabaseName($"UX_{TableName}_Member");
    }
}
