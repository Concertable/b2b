namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record RejectVerificationRequest
{
    public required string Reason { get; init; }
}
