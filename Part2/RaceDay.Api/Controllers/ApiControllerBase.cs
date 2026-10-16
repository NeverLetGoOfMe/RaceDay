using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The id of the logged-in user, taken from the session.</summary>
    protected int CurrentUserId => HttpContext.Session.GetUserId() ?? 0;

    protected ObjectResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message });
}
