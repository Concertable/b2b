using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Authorization.Infrastructure.Services;

internal sealed record MembershipResolution(MembershipSnapshot? Membership);

internal interface IMembershipContextAccessor
{
    MembershipResolution? Resolution { get; set; }
}

internal sealed class MembershipContextAccessor : IMembershipContextAccessor
{
    private const string ItemKey = "Concertable.Authorization.Resolution";

    private readonly IHttpContextAccessor httpContextAccessor;

    public MembershipContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public MembershipResolution? Resolution
    {
        get => httpContextAccessor.HttpContext is { } http && http.Items.TryGetValue(ItemKey, out var value)
            ? value as MembershipResolution
            : null;
        set
        {
            if (httpContextAccessor.HttpContext is not { } http)
                throw new InvalidOperationException(
                    "A membership resolution has no request to belong to. Established execution scopes resolve nothing.");

            http.Items[ItemKey] = value;
        }
    }
}
