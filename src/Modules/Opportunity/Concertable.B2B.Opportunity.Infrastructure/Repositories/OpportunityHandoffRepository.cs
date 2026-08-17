using Concertable.B2B.Opportunity.Infrastructure.Data;

namespace Concertable.B2B.Opportunity.Infrastructure.Repositories;

internal sealed class OpportunityHandoffRepository(OpportunityHandoffDbContext context)
    : Repository<OpportunityEntity, int>(context), IOpportunityHandoffRepository;
