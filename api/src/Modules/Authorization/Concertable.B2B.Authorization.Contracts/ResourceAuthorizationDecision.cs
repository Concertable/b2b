using System.Diagnostics.CodeAnalysis;

namespace Concertable.B2B.Authorization.Contracts;

public readonly record struct ResourceAuthorizationDecision
{
    private ResourceAuthorizationDecision(ResourceAuthorizationEvidence evidence)
    {
        this.Evidence = evidence;
    }

    public static ResourceAuthorizationDecision Denied => default;

    public ResourceAuthorizationEvidence? Evidence { get; }

    [MemberNotNullWhen(true, nameof(Evidence))]
    public bool IsAllowed => Evidence is not null;

    public static ResourceAuthorizationDecision From(ResourceAuthorizationEvidence? evidence) =>
        evidence is null ? Denied : new ResourceAuthorizationDecision(evidence);
}
