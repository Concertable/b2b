using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Application.Infrastructure.Specifications;

internal sealed class ApplicationSpecification : SpecificationBuilder<ApplicationEntity>
{
    public static ISpecification<ApplicationEntity, int?> CreateOpportunityId() =>
        new ApplicationSpecification().Select(application => (int?)application.OpportunityId);

    /* The two economic sides of an application, projected without loading the row. They are this module's own
       financial facts, not an access rule — who may read the application is its grants. */
    public static ISpecification<ApplicationEntity, ApplicationTenants?> CreateTenants() =>
        new ApplicationSpecification().Select(application =>
            new ApplicationTenants(application.VenueTenantId, application.ArtistTenantId));

    public static ISpecification<ApplicationEntity, Guid?> CreateVenueTenantId() =>
        new ApplicationSpecification().Select(application => (Guid?)application.VenueTenantId);
}
