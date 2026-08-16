using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Reunion;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IContentReportService
{
    /// <summary>Records a report against the message and sends the safety-inbox notification plus the
    /// reporter's acknowledgement. A message the acting tenant is not party to is indistinguishable from
    /// one that does not exist.</summary>
    Task<UnitResult<ReportMessageError>> SubmitAsync(int messageId, ReportMessageRequest request);
}
