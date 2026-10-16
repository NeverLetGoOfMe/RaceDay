using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Registers a new account as an Organiser or a Participant.</summary>
    /// <remarks>The password is hashed before it is stored. Role must be "Organiser" or "Participant".</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Invalid data or unknown role.</response>
    /// <response code="409">That email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.Role);
        if (role == null)
        {
            return BadRequest(new { message = "Role must be Organiser or Participant." });
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "That email address is already registered." });
        }

        var user = new User
        {
            RoleId = role.RoleId,
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            ContactNumber = dto.ContactNumber
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me", new UserResponseDto(user.UserId, user.FullName, user.Email, user.ContactNumber, role.RoleName));
    }

    /// <summary>Logs in and starts a session that remembers the user's ID and role.</summary>
    /// <response code="200">Logged in.</response>
    /// <response code="401">Wrong email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        HttpContext.Session.SetUser(user.UserId, user.Role!.RoleName);
        return Ok(new { message = "Logged in.", userId = user.UserId, role = user.Role.RoleName });
    }

    /// <summary>Ends the current session.</summary>
    /// <response code="200">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}
