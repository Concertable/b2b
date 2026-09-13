using Concertable.B2B.Conversations.Domain.Enums;
using Concertable.Kernel;

namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class ContentReportEntityTests
{
    private static readonly Guid VenueTenantId = Guid.NewGuid();
    private static readonly Guid ArtistTenantId = Guid.NewGuid();

    private static MessageEntity Message(string content = "reported content") =>
        MessageEntity.Create(VenueTenantId, ArtistTenantId, senderTenantId: ArtistTenantId,
            sentByUserId: Guid.NewGuid(), content, new DateTime(2026, 1, 1));

    [Fact]
    public void Create_SnapshotsThePairReporterAndReportedParty()
    {
        var reportedByUserId = Guid.NewGuid();
        var submittedAt = new DateTime(2026, 8, 14, 10, 30, 0);

        var report = ContentReportEntity.Create(Message(), VenueTenantId, reportedByUserId,
            ReportCategory.IllegalContent, "please review", submittedAt);

        Assert.Equal(VenueTenantId, report.VenueTenantId);
        Assert.Equal(ArtistTenantId, report.ArtistTenantId);
        Assert.Equal(VenueTenantId, report.ReporterTenantId);
        Assert.Equal(ArtistTenantId, report.ReportedTenantId);
        Assert.Equal(reportedByUserId, report.ReportedByUserId);
        Assert.Equal(ReportCategory.IllegalContent, report.Category);
        Assert.Equal("please review", report.Details);
        Assert.Equal("reported content", report.MessageExcerpt);
        Assert.Equal(submittedAt, report.SubmittedAt);
        Assert.Null(report.Outcome);
        Assert.Null(report.ResolvedAt);
        Assert.Null(report.ResolvedByUserId);
    }

    [Fact]
    public void Create_TruncatesTheExcerptToTheSnapshotLimit()
    {
        var content = new string('x', ContentReportEntity.MaxExcerptLength + 50);

        var report = ContentReportEntity.Create(Message(content), VenueTenantId, Guid.NewGuid(),
            ReportCategory.Other, null, new DateTime(2026, 8, 14));

        Assert.Equal(ContentReportEntity.MaxExcerptLength, report.MessageExcerpt.Length);
    }

    [Fact]
    public void Resolve_StampsTheOutcomeActorAndTime()
    {
        var report = ContentReportEntity.Create(Message(), VenueTenantId, Guid.NewGuid(),
            ReportCategory.Harassment, null, new DateTime(2026, 8, 14));
        var resolvedByUserId = Guid.NewGuid();
        var resolvedAt = new DateTime(2026, 8, 15, 9, 0, 0);

        report.Resolve(ReportOutcome.ContentRemoved, resolvedByUserId, "message hidden", resolvedAt);

        Assert.Equal(ReportOutcome.ContentRemoved, report.Outcome);
        Assert.Equal(resolvedByUserId, report.ResolvedByUserId);
        Assert.Equal("message hidden", report.ResolutionNotes);
        Assert.Equal(resolvedAt, report.ResolvedAt);
    }

    [Fact]
    public void Resolve_Twice_Throws()
    {
        var report = ContentReportEntity.Create(Message(), VenueTenantId, Guid.NewGuid(),
            ReportCategory.Fraud, null, new DateTime(2026, 8, 14));
        report.Resolve(ReportOutcome.NoActionTaken, Guid.NewGuid(), null, new DateTime(2026, 8, 15));

        Assert.Throws<DomainException>(() =>
            report.Resolve(ReportOutcome.ContentRemoved, Guid.NewGuid(), null, new DateTime(2026, 8, 16)));
    }
}
