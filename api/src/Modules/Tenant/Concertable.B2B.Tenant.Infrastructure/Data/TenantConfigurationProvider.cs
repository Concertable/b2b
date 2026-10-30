using Concertable.DataAccess.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class TenantConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationCatalogStateConfiguration());
        modelBuilder.ApplyConfiguration(new TenantBusinessActivityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMembershipEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantRoleDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new TenantRolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new MembershipRoleAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationRoleAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new TenantInvitationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantActivityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantVerificationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VerificationDocumentEntityConfiguration());
    }
}
