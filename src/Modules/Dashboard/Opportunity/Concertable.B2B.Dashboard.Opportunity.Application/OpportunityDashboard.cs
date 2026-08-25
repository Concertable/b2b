using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Dashboard.Opportunity.Application;

internal sealed record OpportunitySummary(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlySet<Genre> Genres,
    DealDto Deal);

internal sealed record OpportunityApplicationMetrics(
    OpportunitySummary Opportunity,
    int ApplicationCount,
    int DaysUntilDeadline);

internal sealed record OpportunityMatch(
    OpportunitySummary Opportunity,
    string County,
    string Town,
    int FitScore);
