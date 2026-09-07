using System.Net;
using Concertable.B2B.Application.Application.Requests;
using Concertable.B2B.Application.Contracts;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class SignatureMappers
{
    extension(ESignatureRequest eSignature)
    {
        public ContractSignature ToSignature(Guid userId, DateTime atUtc, IPAddress ip, string? userAgent) =>
            new(userId, atUtc, ip, userAgent, eSignature.SignatoryName, eSignature.DrawnSignatureImage);
    }
}
