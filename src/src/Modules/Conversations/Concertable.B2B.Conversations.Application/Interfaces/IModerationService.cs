using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IModerationService
{
    Task<IPagination<ContentReportDto>> GetQueueAsync(IPageParams pageParams);

    /// <summary>Removes the message from every participant's inbox without deleting its content.</summary>
    Task<UnitResult<ModerationError>> HideMessageAsync(int messageId);

    /// <summary>Reverses a hide — the mechanical half of the appeal right.</summary>
    Task<UnitResult<ModerationError>> RestoreMessageAsync(int messageId);

    Task<UnitResult<ModerationError>> ResolveReportAsync(int reportId, ResolveReportRequest request);
}
