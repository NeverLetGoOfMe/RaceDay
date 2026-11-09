using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Helpers;

/// <summary>
/// Protects a page using the session: not logged in goes to the login page,
/// and the wrong role goes to an "access denied" page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public RoleAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.UserId() == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (_role != null && session.Role() != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
