using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected readonly ApiClient Api;

    protected AppController(ApiClient api)
    {
        Api = api;
    }

    protected int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    protected void Flash(bool success, string message)
    {
        TempData[success ? "Success" : "Error"] = message;
    }

    // A page could not load because the API call failed: expired session goes back to login,
    // anything else shows a friendly message (never raw error text)
    protected IActionResult ApiFailure<T>(ApiResult<T> result)
    {
        if (result.Status == System.Net.HttpStatusCode.Unauthorized)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        return View("~/Views/Shared/ApiError.cshtml", result.Error);
    }
}
