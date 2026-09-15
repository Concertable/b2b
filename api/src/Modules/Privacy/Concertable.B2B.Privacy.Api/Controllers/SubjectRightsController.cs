using Concertable.B2B.Privacy.Application.DTOs;
using Concertable.B2B.Privacy.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.B2B.Privacy.Api.Controllers;

/// <summary>The reachable admin-gated GDPR subject-rights surface: raise an erasure (art. 17) or pull a
/// portable export (arts. 15/20) for a data subject. Gated by the platform-admin <c>Admin</c> policy that the
/// Admin module owns — a DSAR is an operator action, not a self-service one. The polished operator UI is the
/// admin console's tenant when it lands; this is the backend it drives.</summary>
[Authorize(Policy = "Admin")]
[EnableRateLimiting(RateLimitPolicies.Sensitive)]
[ApiController]
internal sealed class SubjectRightsController : ControllerBase
{
    private readonly ISubjectErasureService erasureService;
    private readonly ISubjectExporter exporter;

    public SubjectRightsController(ISubjectErasureService erasureService, ISubjectExporter exporter)
    {
        this.erasureService = erasureService;
        this.exporter = exporter;
    }

    [HttpPost("/api/subject-erasure/{subjectId:guid}")]
    public async Task<ActionResult<SubjectErasureRequestDto>> RequestErasure(Guid subjectId, CancellationToken ct) =>
        Ok(await erasureService.RequestErasureAsync(subjectId, ct));

    [HttpGet("/api/subject-export/{subjectId:guid}")]
    public async Task<IActionResult> Export(Guid subjectId, CancellationToken ct)
    {
        var download = await exporter.ExportAsync(subjectId, ct);
        return File(download.Content, download.ContentType, download.FileName);
    }
}
