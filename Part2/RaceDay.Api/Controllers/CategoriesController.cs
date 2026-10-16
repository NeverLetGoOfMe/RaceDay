using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    private static CategoryResponseDto ToDto(Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.MaxParticipants, c.EntryFee);

    /// <summary>Lists the categories available for an event.</summary>
    /// <response code="200">An array of categories.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("/api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _db.Categories.Where(c => c.EventId == eventId).ToListAsync();
        return Ok(categories.Select(ToDto));
    }

    /// <summary>Adds a category (e.g. "Under 20" or "Senior") to an event the organiser owns.</summary>
    /// <response code="201">The new category.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("/api/events/{eventId:int}/categories")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        var e = await _db.Events.FindAsync(eventId);
        if (e == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (e.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only add categories to your own events.");
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", ToDto(category));
    }

    /// <summary>Edits a category on one of the organiser's events.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="403">The category belongs to another organiser's event.</response>
    /// <response code="404">No category with that id.</response>
    [HttpPut("/api/categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event!.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only edit categories on your own events.");
        }

        category.CategoryName = dto.CategoryName.Trim();
        category.MaxParticipants = dto.MaxParticipants;
        category.EntryFee = dto.EntryFee;
        await _db.SaveChangesAsync();

        return Ok(ToDto(category));
    }

    /// <summary>Removes a category from one of the organiser's events.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">The category belongs to another organiser's event.</response>
    /// <response code="404">No category with that id.</response>
    [HttpDelete("/api/categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event!.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete categories on your own events.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
