namespace Concertable.B2B.Conversations.Contracts;

/// <summary>The subject's portable B2B Conversations fragment for a GDPR access/portability export (arts. 15/20):
/// one message body they authored, with its sender tenant and timestamp.</summary>
public sealed record MessageExport
{
    public required string Content { get; init; }
    public Guid SenderTenantId { get; init; }
    public DateTime SentDate { get; init; }
}
