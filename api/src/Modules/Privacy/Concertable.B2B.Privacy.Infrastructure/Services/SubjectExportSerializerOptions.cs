using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concertable.B2B.Privacy.Infrastructure.Services;

internal static class SubjectExportSerializerOptions
{
    public static JsonSerializerOptions Value { get; } =
        new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
}
