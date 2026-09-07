namespace Concertable.B2B.Booking.Application.DTOs;

internal sealed record FileDownload(byte[] Content, string FileName, string ContentType);
