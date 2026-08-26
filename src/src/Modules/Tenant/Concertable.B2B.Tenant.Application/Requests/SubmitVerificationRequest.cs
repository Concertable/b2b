using Concertable.B2B.Tenant.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record SubmitVerificationRequest
{
    public required IFormFileCollection Files { get; init; }
    public required IReadOnlyList<VerificationDocumentType> DocumentTypes { get; init; }
}
