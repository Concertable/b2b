using Concertable.B2B.Opportunity.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal sealed class OpportunityConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new OpportunityEntityConfiguration());
}
