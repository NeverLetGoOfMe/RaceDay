using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private static EventResponseDto ToDto(Event e) =>
        new(e.EventId, e.OrganiserId, e.EventName, e.EventDate, e.Location, e.Description, e.RouteInfo, e.Distance, e.EventType);

    /// <summary>Lists all events, soonest first.</summary>
    /// <response code="200">An array of events.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>Returns the details of one event.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(ToDto(e));
    }

    /// <summary>Creates a new event owned by the logged-in organiser.</summary>
    /// <remarks>EventType must be Run, Walk or Cycle. Distance is in kilometres.</remarks>
    /// <response code="201">The new event.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only organisers can create events.</response>
    [HttpPost]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventDto dto)
    {
        var e = new Event
        {
            OrganiserId = CurrentUserId,
            EventName = dto.EventName.Trim(),
            EventDate = dto.EventDate.Date,
            Location = dto.Location.Trim(),
            Description = dto.Description,
            RouteInfo = dto.RouteInfo,
            Distance = dto.Distance,
            EventType = dto.EventType
        };

        _db.Events.Add(e);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{e.EventId}", ToDto(e));
    }

    /// <summary>Edits an event. Only the organiser who created it can do this.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (e.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only edit your own events.");
        }

        e.EventName = dto.EventName.Trim();
        e.EventDate = dto.EventDate.Date;
        e.Location = dto.Location.Trim();
        e.Description = dto.Description;
        e.RouteInfo = dto.RouteInfo;
        e.Distance = dto.Distance;
        e.EventType = dto.EventType;
        await _db.SaveChangesAsync();

        return Ok(ToDto(e));
    }

    /// <summary>Deletes an event and its categories. Only the organiser who created it can do this.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event with that id.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (e.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete your own events.");
        }

        _db.Events.Remove(e);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
