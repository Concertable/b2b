namespace Concertable.B2B.DataAccess.Application;

public sealed record MemberAssignment(Guid MembershipId, Guid AssignedByUserId, DateTime AssignedAt);
