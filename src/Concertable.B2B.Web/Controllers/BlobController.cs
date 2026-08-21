using Concertable.Shared.Imaging.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Concertable.B2B.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BlobController : ControllerBase
{
    private readonly IImageService imageService;

    public BlobController(IImageService imageService)
    {
        this.imageService = imageService;
    }

    [AllowAnonymous]
    [HttpGet("download/{blobName}")]
    public async Task<IActionResult> Download(string blobName)
    {
        var stream = await imageService.DownloadAsync(blobName);

        if (stream == null)
            return NotFound("Blob not found");

        var contentType = GetContentType(blobName);

        return File(stream, contentType, blobName);
    }

    private string GetContentType(string fileName)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
}
