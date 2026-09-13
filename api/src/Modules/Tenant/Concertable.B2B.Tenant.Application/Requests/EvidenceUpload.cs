using Concertable.B2B.Tenant.Domain.Enums;

namespace Concertable.B2B.Tenant.Application.Requests;

/// <summary>One evidence file for <see cref="IVerificationService.SubmitAsync"/>, deliberately free of
/// ASP.NET Core's <c>IFormFile</c> so the service stays callable outside an HTTP request.</summary>
internal sealed record EvidenceUpload(Stream Content, string FileExtension, VerificationDocumentType DocumentType);
