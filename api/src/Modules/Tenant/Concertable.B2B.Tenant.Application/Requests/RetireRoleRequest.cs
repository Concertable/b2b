namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record RetireRoleRequest(long ExpectedVersion, Guid? ReplacementRoleId);
