namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IContentReportNotifier
{
    Task SubmittedAsync(ContentReportEntity report);
}
