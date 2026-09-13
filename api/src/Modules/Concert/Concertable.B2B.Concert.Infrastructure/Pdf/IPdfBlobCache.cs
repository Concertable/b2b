using QuestPDF.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Pdf;

/// <summary>
/// Renders a document to a PDF blob on first request and serves the stored blob thereafter. The
/// render itself is serialized inside <see cref="Shared.Pdf.Application.IPdfRenderer"/> (QuestPDF is
/// not thread-safe), so this cache adds no locking of its own.
/// </summary>
internal interface IPdfBlobCache
{
    Task<byte[]> GetOrCreateAsync(string blobName, IDocument document, CancellationToken ct = default);
}
