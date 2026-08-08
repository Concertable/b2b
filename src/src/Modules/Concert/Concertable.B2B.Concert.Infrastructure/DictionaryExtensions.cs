using System.Globalization;

namespace Concertable.B2B.Concert.Infrastructure;

internal static class DictionaryExtensions
{
    /// <summary>Returns the value for <paramref name="key"/>; throws if it is absent or empty.</summary>
    internal static string GetValue(this IReadOnlyDictionary<string, string> source, string key) =>
        source.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : throw new InvalidOperationException($"Required key '{key}' was not present.");

    /// <summary>Returns the value for <paramref name="key"/> parsed as <typeparamref name="T"/>; throws if absent, empty, or unparsable.</summary>
    internal static T GetValueAs<T>(this IReadOnlyDictionary<string, string> source, string key) where T : IParsable<T>
    {
        var raw = source.GetValue(key);
        return T.TryParse(raw, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidOperationException($"Key '{key}' value '{raw}' is not a valid {typeof(T).Name}.");
    }
}
