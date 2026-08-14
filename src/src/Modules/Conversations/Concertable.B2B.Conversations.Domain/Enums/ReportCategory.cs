using System.Text.Json.Serialization;

namespace Concertable.B2B.Conversations.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportCategory
{
    IllegalContent,
    Harassment,
    Fraud,
    Spam,
    Other
}
