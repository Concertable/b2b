using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantActivityEntity : IGuidEntity
{
    private TenantActivityEntity() { }

    public Guid Id { get; private set; }
    public string SourceKey { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public ActivityType Type { get; private set; }
    public DateTimeOffset At { get; private set; }
    public string Subject { get; private set; } = null!;
    public string? Detail { get; private set; }
    public string Url { get; private set; } = null!;

    public static TenantActivityEntity Create(ActivityRecord record) => new()
    {
        Id = Guid.CreateVersion7(record.At),
        SourceKey = record.SourceKey,
        TenantId = record.TenantId,
        Type = record.Type,
        At = record.At,
        Subject = Truncate(record.Subject, 500),
        Detail = record.Detail is null ? null : Truncate(record.Detail, 1000),
        Url = record.Url
    };

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength
            ? value
            : $"{value[..(maximumLength - 3)]}...";
}
