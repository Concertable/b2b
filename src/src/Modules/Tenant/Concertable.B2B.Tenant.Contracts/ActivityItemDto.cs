using System.Text.Json.Serialization;

namespace Concertable.B2B.Tenant.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityType
{
    ApplicationReceived,
    ApplicationAccepted,
    ApplicationDeclined,
    ApplicationWithdrawn,
    ApplicationCancelled,
    ConcertSettled,
    ReviewReceived,
    TicketSold,
    MessageReceived
}

public sealed record ActivityItemDto(
    Guid Id,
    ActivityType Type,
    DateTimeOffset At,
    string Subject,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Detail,
    string Url);

public sealed record ActivityRecord(
    string SourceKey,
    Guid TenantId,
    ActivityType Type,
    DateTimeOffset At,
    string Subject,
    string? Detail,
    string Url);
