namespace Concertable.B2B.Privacy.Application.DTOs;

internal sealed record FileDownload(byte[] Content, string FileName, string ContentType);
