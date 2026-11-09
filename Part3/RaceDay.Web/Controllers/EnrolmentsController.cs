using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class EnrolmentsController : AppController
{
    public EnrolmentsController(ApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> My()
    {
        var result = await Api.GetAsync<List<ApiEnrolment>>("api/enrolments/me");
        if (!result.Success)
        {
            return ApiFailure(result);
        }

        return View(result.Data ?? new List<ApiEnrolment>());
    }
}
