using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Application.Infrastructure.Specifications;

internal sealed class ApplicationSpecification : SpecificationBuilder<ApplicationEntity>
{
    public static ISpecification<ApplicationEntity, int?> CreateOpportunityId() =>
        new ApplicationSpecification().Select(application => (int?)application.OpportunityId);

    public static ISpecification<ApplicationEntity, Guid?> CreateVenueTenantId() =>
        new ApplicationSpecification().Select(application => (Guid?)application.VenueTenantId);
}
