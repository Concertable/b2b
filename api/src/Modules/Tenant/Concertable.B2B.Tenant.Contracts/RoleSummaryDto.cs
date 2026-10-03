namespace Concertable.B2B.Tenant.Contracts;

public sealed record RoleSummaryDto(Guid Id, string Name, bool IsProtectedOwner);
