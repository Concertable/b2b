using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Reunion;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IContentReportService
{
    Task<UnitResult<ReportMessageError>> SubmitAsync(
        int conversationId,
        int messageId,
        ReportMessageRequest request);
}
