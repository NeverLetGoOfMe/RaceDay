using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class ResultsController : AppController
{
    public ResultsController(ApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> My()
    {
        var result = await Api.GetAsync<List<ApiRaceResult>>("api/results/me");
        if (!result.Success)
        {
            return ApiFailure(result);
        }

        return View((result.Data ?? new List<ApiRaceResult>()).OrderByDescending(r => r.EventDate).ToList());
    }
}
