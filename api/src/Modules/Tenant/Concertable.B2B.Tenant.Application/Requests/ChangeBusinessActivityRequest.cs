namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record ChangeBusinessActivityRequest
{
    public required long ExpectedEligibilityVersion { get; init; }
}
