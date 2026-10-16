using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users/me")]
[SessionAuthorize]
public class UsersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the profile of the logged-in user.</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(new UserResponseDto(user.UserId, user.FullName, user.Email, user.ContactNumber, user.Role!.RoleName));
    }

    /// <summary>Updates the name and contact number of the logged-in user.</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.FullName = dto.FullName.Trim();
        user.ContactNumber = dto.ContactNumber;
        await _db.SaveChangesAsync();

        return Ok(new UserResponseDto(user.UserId, user.FullName, user.Email, user.ContactNumber, user.Role!.RoleName));
    }
}
