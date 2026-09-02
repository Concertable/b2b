using System.Net;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.ValueObjects;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class SignatureMappers
{
    extension(Signature signature)
    {
        public SignatureDto ToDto() =>
            new(
                signature.UserId,
                signature.AtUtc,
                signature.Ip,
                signature.UserAgent,
                signature.SignatoryName,
                signature.DrawnSignatureImage);
    }

    extension(ESignatureRequest eSignature)
    {
        public Signature ToSignature(Guid userId, DateTime atUtc, IPAddress ip, string? userAgent) =>
            new(userId, atUtc, ip, userAgent, eSignature.SignatoryName, eSignature.DrawnSignatureImage);
    }
}
