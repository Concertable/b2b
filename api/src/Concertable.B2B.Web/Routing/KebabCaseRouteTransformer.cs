using System.Text;
using Microsoft.AspNetCore.Routing;

namespace Concertable.B2B.Web.Routing;

internal sealed class KebabCaseRouteTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        var routeValue = value?.ToString();
        if (string.IsNullOrEmpty(routeValue))
            return routeValue;

        var transformed = new StringBuilder(routeValue.Length + 4);
        for (var index = 0; index < routeValue.Length; index++)
        {
            var current = routeValue[index];
            if (index > 0
                && char.IsUpper(current)
                && (char.IsLower(routeValue[index - 1])
                    || char.IsDigit(routeValue[index - 1])
                    || index + 1 < routeValue.Length && char.IsLower(routeValue[index + 1])))
                transformed.Append('-');

            transformed.Append(char.ToLowerInvariant(current));
        }

        return transformed.ToString();
    }
}
