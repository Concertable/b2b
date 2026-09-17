using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// Maps Tenant's authority view keyless into a resource module's context, so an access predicate can re-check
/// the caller's membership incarnation and permission revision in the same statement that reads the resource.
/// Keyless is the write protection: EF cannot track or save these rows at all.
/// </summary>
internal sealed class MembershipAuthorityConfiguration : IEntityTypeConfiguration<MembershipAuthority>
{
    private const string TenantSchema = "tenant";
    internal const string ViewName = "MembershipAuthority";

    public void Configure(EntityTypeBuilder<MembershipAuthority> builder)
    {
        builder.HasNoKey();
        builder.ToView(ViewName, TenantSchema);
    }
}
