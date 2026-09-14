using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Events;

public sealed record TenantVerificationChangedDomainEvent(
    Guid TenantVerificationId,
    Guid TenantId,
    TenantVerificationStatus Status,
    string? RejectionReason,
    Guid? ReviewedByAdminSub,
    DateTime? ReviewedAt) : IDomainEvent;
