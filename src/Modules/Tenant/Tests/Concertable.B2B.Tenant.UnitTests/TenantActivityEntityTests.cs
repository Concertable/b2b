using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantActivityEntityTests
{
    [Fact]
    public void Create_PreservesActivityRecord()
    {
        var tenantId = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 8, 14, 12, 30, 0, TimeSpan.Zero);
        var record = new ActivityRecord(
            "message:42",
            tenantId,
            ActivityType.MessageReceived,
            at,
            "New message",
            "Details",
            "/_venue/?inbox=open");

        var activity = TenantActivityEntity.Create(record);

        Assert.NotEqual(Guid.Empty, activity.Id);
        Assert.Equal(record.SourceKey, activity.SourceKey);
        Assert.Equal(record.TenantId, activity.TenantId);
        Assert.Equal(record.Type, activity.Type);
        Assert.Equal(record.At, activity.At);
        Assert.Equal(record.Subject, activity.Subject);
        Assert.Equal(record.Detail, activity.Detail);
        Assert.Equal(record.Url, activity.Url);
    }

    [Fact]
    public void Create_TruncatesDisplayTextToPersistenceLimits()
    {
        var record = new ActivityRecord(
            "message:42",
            Guid.NewGuid(),
            ActivityType.MessageReceived,
            DateTimeOffset.UtcNow,
            new string('s', 501),
            new string('d', 1001),
            "/_venue/?inbox=open");

        var activity = TenantActivityEntity.Create(record);

        Assert.Equal(500, activity.Subject.Length);
        Assert.Equal(1000, activity.Detail!.Length);
        Assert.EndsWith("...", activity.Subject);
        Assert.EndsWith("...", activity.Detail);
    }
}
