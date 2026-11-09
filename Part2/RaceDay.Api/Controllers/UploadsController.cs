using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class UploadsController : ApiControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UploadsController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    private async Task<(string? Name, IActionResult? Error)> StoreAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return (null, BadRequest(new { message = "Please choose an image file." }));
        }

        if (file.Length > MaxBytes)
        {
            return (null, BadRequest(new { message = "The image must be smaller than 5 MB." }));
        }

        if (!AllowedTypes.TryGetValue(file.ContentType, out var extension))
        {
            return (null, BadRequest(new { message = "Only JPEG, PNG or WebP images are allowed." }));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var name = await _blobs.UploadAsync(stream, file.ContentType, extension);
            return (name, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is Azure.RequestFailedException)
        {
            return (null, StatusCode(503, new { message = "Image storage is not available right now." }));
        }
    }

    /// <summary>Uploads a banner image for one of the organiser's events (stored in Azure Blob Storage).</summary>
    /// <remarks>Send the image as multipart/form-data in a field called "file". JPEG, PNG or WebP, up to 5 MB.</remarks>
    /// <response code="200">The stored image name.</response>
    /// <response code="400">Missing, too large or wrong type of file.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="503">Image storage is not available.</response>
    [HttpPost("/api/events/{eventId:int}/banner")]
    [SessionAuthorize(RoleNames.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile file)
    {
        var e = await _db.Events.FindAsync(eventId);
        if (e == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (e.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only change the banner of your own events.");
        }

        var (name, error) = await StoreAsync(file);
        if (error != null)
        {
            return error;
        }

        e.BannerImage = name;
        await _db.SaveChangesAsync();
        return Ok(new { image = name });
    }

    /// <summary>Uploads a profile picture for the logged-in user (stored in Azure Blob Storage).</summary>
    /// <remarks>Send the image as multipart/form-data in a field called "file". JPEG, PNG or WebP, up to 5 MB.</remarks>
    /// <response code="200">The stored image name.</response>
    /// <response code="400">Missing, too large or wrong type of file.</response>
    /// <response code="503">Image storage is not available.</response>
    [HttpPost("/api/users/me/picture")]
    [SessionAuthorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var (name, error) = await StoreAsync(file);
        if (error != null)
        {
            return error;
        }

        user.ProfilePicture = name;
        await _db.SaveChangesAsync();
        return Ok(new { image = name });
    }

    /// <summary>Returns a stored image so the website can display it. Images are not directly public in Azure.</summary>
    /// <response code="200">The image file.</response>
    /// <response code="404">No image with that name.</response>
    [HttpGet("/api/images/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetImage(string name)
    {
        if (!Regex.IsMatch(name, @"^[a-f0-9]{32}\.(jpg|png|webp)$"))
        {
            return NotFound(new { message = "Image not found." });
        }

        try
        {
            var image = await _blobs.OpenReadAsync(name);
            if (image == null)
            {
                return NotFound(new { message = "Image not found." });
            }

            return File(image.Value.Content, image.Value.ContentType);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is Azure.RequestFailedException)
        {
            return StatusCode(503, new { message = "Image storage is not available right now." });
        }
    }
}
