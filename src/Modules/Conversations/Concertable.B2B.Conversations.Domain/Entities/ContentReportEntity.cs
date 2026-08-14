using Concertable.B2B.Conversations.Domain.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

/// <summary>
/// An illegal-content report raised against one <see cref="MessageEntity"/>, and the moderation outcome
/// recorded against it. <see cref="MessageExcerpt"/> is a snapshot taken at report time so the record
/// still evidences what was reported after the message itself is hidden.
/// </summary>
public sealed class ContentReportEntity : IIdEntity, IVenueArtistTenantScoped
{
    public const int MaxExcerptLength = 500;

    private ContentReportEntity() { }

    public int Id { get; private set; }
    public int MessageId { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public Guid ReporterTenantId { get; private set; }
    public Guid ReportedTenantId { get; private set; }
    public Guid ReportedByUserId { get; private set; }
    public ReportCategory Category { get; private set; }
    public string? Details { get; private set; }
    public string MessageExcerpt { get; private set; } = null!;
    public DateTime SubmittedAt { get; private set; }
    public ReportOutcome? Outcome { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public static ContentReportEntity Create(
        MessageEntity message,
        Guid reporterTenantId,
        Guid reportedByUserId,
        ReportCategory category,
        string? details,
        DateTime submittedAt) => new()
        {
            MessageId = message.Id,
            VenueTenantId = message.VenueTenantId,
            ArtistTenantId = message.ArtistTenantId,
            ReporterTenantId = reporterTenantId,
            ReportedTenantId = message.SenderTenantId,
            ReportedByUserId = reportedByUserId,
            Category = category,
            Details = details,
            MessageExcerpt = Excerpt(message.Content),
            SubmittedAt = submittedAt
        };

    public void Resolve(ReportOutcome outcome, Guid resolvedByUserId, string? notes, DateTime at)
    {
        if (Outcome is not null)
            throw new DomainException("This report has already been resolved.");

        Outcome = outcome;
        ResolvedByUserId = resolvedByUserId;
        ResolutionNotes = notes;
        ResolvedAt = at;
    }

    private static string Excerpt(string content) =>
        content.Length <= MaxExcerptLength ? content : content[..MaxExcerptLength];
}
