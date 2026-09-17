using Concertable.B2B.Admin.Api.Authorization;
using Concertable.B2B.Privacy.Application.DTOs;
using Concertable.B2B.Privacy.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Privacy.Api.Controllers;

/// <summary>The reachable admin-gated GDPR subject-rights surface: raise an erasure (art. 17) or pull a
/// portable export (arts. 15/20) for a data subject. A DSAR is an operator action, not a self-service one.</summary>
[Admin]
[EnableRateLimiting(RateLimitPolicies.Sensitive)]
[ApiController]
[Route("api")]
internal sealed class SubjectRightsController : ControllerBase
{
    private readonly ISubjectErasureService erasureService;
    private readonly ISubjectExporter exporter;

    public SubjectRightsController(ISubjectErasureService erasureService, ISubjectExporter exporter)
    {
        this.erasureService = erasureService;
        this.exporter = exporter;
    }

    [HttpPost("subject-erasure/{subjectId:guid}")]
    public async Task<ActionResult<SubjectErasureRequestDto>> RequestErasure(Guid subjectId, CancellationToken ct) =>
        (await erasureService.RequestErasureAsync(subjectId, ct)).ToOkOrProblem();

    [HttpGet("subject-export/{subjectId:guid}")]
    public async Task<IActionResult> Export(Guid subjectId, CancellationToken ct)
    {
        var download = await exporter.ExportAsync(subjectId, ct);
        return File(download.Content, download.ContentType, download.FileName);
    }
}
