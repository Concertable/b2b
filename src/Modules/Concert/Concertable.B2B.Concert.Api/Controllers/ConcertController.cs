using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.User.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class ConcertController : ControllerBase
{
    private readonly IConcertService concertService;

    public ConcertController(IConcertService concertService)
    {
        this.concertService = concertService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConcertDetailsResponse>> GetDetailsById(int id)
    {
        return Ok((await concertService.GetDetailsByIdAsync(id)).ToDetailsResponse());
    }

    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<ConcertDetailsResponse>> GetDetailsByApplicationId(int applicationId)
    {
        return Ok((await concertService.GetDetailsByApplicationIdAsync(applicationId)).ToDetailsResponse());
    }

    [HttpGet("upcoming/venue/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUpcomingByVenueId(int id)
    {
        return Ok((await concertService.GetUpcomingByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("upcoming/artist/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUpcomingByArtistId(int id)
    {
        return Ok((await concertService.GetUpcomingByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("history/venue/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetHistoryByVenueId(int id)
    {
        return Ok((await concertService.GetHistoryByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("history/artist/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetHistoryByArtistId(int id)
    {
        return Ok((await concertService.GetHistoryByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("unposted/venue/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUnpostedByVenueId(int id)
    {
        return Ok((await concertService.GetUnpostedByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("unposted/artist/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUnpostedByArtistId(int id)
    {
        return Ok((await concertService.GetUnpostedByArtistIdAsync(id)).ToSummaryResponses());
    }

    [VenueManager]
    [HttpPut("{id}")]
    public async Task<ActionResult<ConcertUpdateResponse>> Update(int id, [FromBody] UpdateConcertRequest request)
    {
        return Ok(await concertService.UpdateAsync(id, request));
    }

    [VenueManager]
    [HttpPut("post/{id}")]
    public async Task<IActionResult> Post(int id, [FromBody] UpdateConcertRequest request)
    {
        await concertService.PostAsync(id, request);
        return NoContent();
    }
}
