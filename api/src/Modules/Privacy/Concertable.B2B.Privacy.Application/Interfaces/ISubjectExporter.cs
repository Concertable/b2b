namespace Concertable.B2B.Privacy.Application.Interfaces;

internal interface ISubjectExporter
{
    Task<FileDownload> ExportAsync(Guid subjectId, CancellationToken ct = default);
}
