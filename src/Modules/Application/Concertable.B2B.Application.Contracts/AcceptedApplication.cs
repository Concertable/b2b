using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.Contracts;

public sealed record AcceptedApplication(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    DealType DealType);
