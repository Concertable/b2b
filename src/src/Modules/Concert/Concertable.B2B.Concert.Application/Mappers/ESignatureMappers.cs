using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class ESignatureMappers
{
    public static ESignatureDto ToDto(this ESignature e) =>
        new(e.UserId, e.AtUtc, e.SignatoryName);
}
