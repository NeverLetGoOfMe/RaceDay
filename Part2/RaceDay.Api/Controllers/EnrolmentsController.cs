using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private static EnrolmentResponseDto ToDto(Enrolment e) =>
        new(e.EnrolmentId, e.ParticipantId, e.Participant!.FullName, e.Category!.EventId, e.Category.Event!.EventName,
            e.Category.Event.EventDate, e.CategoryId, e.Category.CategoryName, e.EnrolmentDate, e.Status);

    /// <summary>Enters the logged-in participant into an event by choosing one of its categories.</summary>
    /// <response code="201">The enrolment that was recorded.</response>
    /// <response code="403">Only participants can enrol.</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [HttpPost("/api/enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        var userId = CurrentUserId;
        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == userId && e.CategoryId == dto.CategoryId))
        {
            return Conflict(new { message = "You are already enrolled in this category." });
        }

        var taken = await _db.Enrolments.CountAsync(e => e.CategoryId == dto.CategoryId);
        if (taken >= category.MaxParticipants)
        {
            return Conflict(new { message = "This category is full." });
        }

        var enrolment = new Enrolment { ParticipantId = userId, CategoryId = dto.CategoryId };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var saved = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category!).ThenInclude(c => c.Event)
            .FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);

        return Created("/api/enrolments/me", ToDto(saved));
    }

    /// <summary>Lists the logged-in participant's own enrolments.</summary>
    /// <response code="200">An array of enrolments.</response>
    /// <response code="403">Only participants have enrolments of their own.</response>
    [HttpGet("/api/enrolments/me")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;
        var list = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category!).ThenInclude(c => c.Event)
            .Where(e => e.ParticipantId == userId)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    /// <summary>Lists every enrolment for one of the organiser's events.</summary>
    /// <response code="200">An array of enrolments.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("/api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var e = await _db.Events.FindAsync(eventId);
        if (e == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (e.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only view enrolments for your own events.");
        }

        var list = await _db.Enrolments
            .Include(x => x.Participant)
            .Include(x => x.Category!).ThenInclude(c => c.Event)
            .Where(x => x.Category!.EventId == eventId)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }
}
