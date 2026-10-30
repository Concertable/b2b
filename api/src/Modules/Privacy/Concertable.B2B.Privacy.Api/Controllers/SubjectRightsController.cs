using Concertable.B2B.Admin.Api.Authorization;
using Concertable.B2B.Privacy.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.B2B.Privacy.Api.Controllers;

[Admin]
[EnableRateLimiting(RateLimitPolicies.Sensitive)]
[ApiController]
[Route("api")]
internal sealed class SubjectRightsController : ControllerBase
{
    private readonly ISubjectExporter exporter;

    public SubjectRightsController(ISubjectExporter exporter)
    {
        this.exporter = exporter;
    }

    [HttpGet("subject-export/{subjectId:guid}")]
    public async Task<IActionResult> Export(Guid subjectId, CancellationToken ct)
    {
        var download = await this.exporter.ExportAsync(subjectId, ct);
        return File(download.Content, download.ContentType, download.FileName);
    }
}
