using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Mappers;

internal static class ContentReportMappers
{
    public static ContentReportDto ToDto(this ContentReportEntity report) => new()
    {
        Id = report.Id,
        Reference = report.Reference,
        MessageId = report.MessageId,
        ReporterTenantId = report.ReporterTenantId,
        ReportedTenantId = report.ReportedTenantId,
        Category = report.Category,
        Details = report.Details,
        MessageExcerpt = report.MessageExcerpt,
        SubmittedAt = report.SubmittedAt,
        Outcome = report.Outcome,
        ResolvedAt = report.ResolvedAt,
        ResolutionNotes = report.ResolutionNotes
    };
}
