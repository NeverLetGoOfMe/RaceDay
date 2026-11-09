using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageEnrolmentsController : AppController
{
    public ManageEnrolmentsController(ApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await Api.GetAsync<ApiEvent>($"api/events/{eventId}");
        if (!ev.Success || ev.Data == null)
        {
            return ApiFailure(ev);
        }

        if (ev.Data.OrganiserId != CurrentUserId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var enrolments = await Api.GetAsync<List<ApiEnrolment>>($"api/events/{eventId}/enrolments");
        if (!enrolments.Success)
        {
            return ApiFailure(enrolments);
        }

        return View(new EventEnrolmentsVm { Event = ev.Data, Enrolments = enrolments.Data ?? new() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int enrolmentId, int eventId, string status)
    {
        var result = await Api.PutAsync<ApiEnrolment>($"api/enrolments/{enrolmentId}/status", new { status });
        Flash(result.Success, result.Success ? $"Enrolment marked as {status}." : result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
