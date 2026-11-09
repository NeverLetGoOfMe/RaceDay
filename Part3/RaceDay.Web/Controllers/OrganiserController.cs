using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : AppController
{
    public OrganiserController(ApiClient api) : base(api)
    {
    }

    // Summary of the organiser's events, total enrolments per event and upcoming dates
    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetAsync<List<ApiEventSummary>>("api/events/mine");
        if (!result.Success)
        {
            return ApiFailure(result);
        }

        return View(result.Data ?? new List<ApiEventSummary>());
    }
}
