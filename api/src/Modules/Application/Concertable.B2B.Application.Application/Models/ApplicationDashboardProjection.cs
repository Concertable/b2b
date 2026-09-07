using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Deal.Domain;
using Concertable.B2B.Enums;

namespace Concertable.B2B.Application.Application.Models;

internal sealed record ApplicationDashboardProjection(
    int OpportunityId,
    ApplicationState State,
    DealType DealType);
