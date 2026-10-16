using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Helpers;

/// <summary>
/// Protects an endpoint using the session. With no role it only requires a logged-in user;
/// with a role it also requires that role (401 if not logged in, 403 if the wrong role).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; }

    public SessionAuthorizeAttribute(string? role = null)
    {
        Role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetUserId() == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must log in first." });
            return;
        }

        if (Role != null && session.GetRole() != Role)
        {
            context.Result = new ObjectResult(new { message = $"This action is only available to the {Role} role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
