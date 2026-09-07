using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Vocabulary;

namespace Concertable.B2B.Application.Application.Models;

internal sealed record ApplicationDashboardProjection(
    int OpportunityId,
    ApplicationState State,
    DealType DealType);
