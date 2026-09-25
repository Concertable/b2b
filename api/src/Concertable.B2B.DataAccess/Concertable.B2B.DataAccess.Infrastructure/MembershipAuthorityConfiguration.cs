using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure;

// Keyless is the write protection: EF cannot track or save these rows at all.
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
