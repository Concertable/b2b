namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record FileDownload(byte[] Content, string FileName, string ContentType);
