using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private static ResultResponseDto ToDto(Result r) =>
        new(r.ResultId, r.EnrolmentId, r.Enrolment!.Participant!.FullName, r.Enrolment.Category!.Event!.EventName,
            r.Enrolment.Category.CategoryName, r.FinishTime, r.Position);

    private IQueryable<Result> WithDetails() =>
        _db.Results
            .Include(r => r.Enrolment!).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment!).ThenInclude(e => e.Category!).ThenInclude(c => c.Event);

    /// <summary>Captures the finish time and finishing position for an enrolment.</summary>
    /// <remarks>FinishTime is sent as hh:mm:ss, for example 00:52:10.</remarks>
    /// <response code="201">The captured result.</response>
    /// <response code="403">The enrolment belongs to another organiser's event.</response>
    /// <response code="404">No enrolment with that id.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("/api/results")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(ResultDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category!).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Category!.Event!.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only capture results for your own events.");
        }

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == dto.EnrolmentId))
        {
            return Conflict(new { message = "A result has already been captured for this enrolment." });
        }

        var result = new Result
        {
            EnrolmentId = dto.EnrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position,
            CapturedByUserId = CurrentUserId
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        result.Enrolment = enrolment;
        return Created($"/api/categories/{enrolment.CategoryId}/results", ToDto(result));
    }

    /// <summary>Lists the logged-in participant's own results.</summary>
    /// <response code="200">An array of results.</response>
    /// <response code="403">Only participants have results of their own.</response>
    [HttpGet("/api/results/me")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;
        var list = await WithDetails().Where(r => r.Enrolment!.ParticipantId == userId).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    /// <summary>Shows the results for a category, ordered by finishing position.</summary>
    /// <response code="200">An array of results.</response>
    /// <response code="404">No category with that id.</response>
    [HttpGet("/api/categories/{categoryId:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForCategory(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == categoryId))
        {
            return NotFound(new { message = "Category not found." });
        }

        var list = await WithDetails()
            .Where(r => r.Enrolment!.CategoryId == categoryId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }
}
