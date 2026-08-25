using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.Application.Models;

internal sealed record ApplicationDashboardProjection(
    int OpportunityId,
    State State,
    DealType DealType);
