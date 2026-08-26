namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record OpportunityApplicationMetrics(
    OpportunityDto Opportunity,
    int ApplicationCount,
    int DaysUntilDeadline);

internal sealed record OpportunityMatch(
    OpportunityDto Opportunity,
    string County,
    string Town,
    int FitScore);
