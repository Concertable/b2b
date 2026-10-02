namespace Concertable.B2B.Conversations.Contracts;

public sealed record SubjectMessageDto
{
    public required string Content { get; init; }
    public Guid SenderTenantId { get; init; }
    public DateTime SentDate { get; init; }
}
