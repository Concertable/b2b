using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Contracts;

public sealed record TenantResolution(
    MembershipSnapshot Actor,
    bool TargetTenantExists,
    MembershipSnapshot? TargetMembership);
