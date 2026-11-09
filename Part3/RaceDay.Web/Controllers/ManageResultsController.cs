using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageResultsController : AppController
{
    public ManageResultsController(ApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index(int eventId)
    {
        var page = await LoadAsync(eventId);
        if (page.Failure != null)
        {
            return page.Failure;
        }

        return View(page.Model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Capture(ResultFormVm form)
    {
        if (!ModelState.IsValid)
        {
            var page = await LoadAsync(form.EventId);
            if (page.Failure != null)
            {
                return page.Failure;
            }

            page.Model!.Form = form;
            return View("Index", page.Model);
        }

        var result = await Api.PostAsync<ApiRaceResult>("api/results", new
        {
            enrolmentId = form.EnrolmentId,
            finishTime = form.FinishTime!.Value.ToString("c"),
            position = form.Position
        });

        Flash(result.Success, result.Success ? "Result captured and published." : result.Error);
        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }

    private async Task<(ResultsPageVm? Model, IActionResult? Failure)> LoadAsync(int eventId)
    {
        var ev = await Api.GetAsync<ApiEvent>($"api/events/{eventId}");
        if (!ev.Success || ev.Data == null)
        {
            return (null, ApiFailure(ev));
        }

        if (ev.Data.OrganiserId != CurrentUserId)
        {
            return (null, RedirectToAction("AccessDenied", "Account"));
        }

        var enrolments = await Api.GetAsync<List<ApiEnrolment>>($"api/events/{eventId}/enrolments");
        if (!enrolments.Success)
        {
            return (null, ApiFailure(enrolments));
        }

        var categories = await Api.GetAsync<List<ApiCategory>>($"api/events/{eventId}/categories");
        if (!categories.Success)
        {
            return (null, ApiFailure(categories));
        }

        // Collect the results already captured for each category of this event
        var byEnrolment = new Dictionary<int, ApiRaceResult>();
        foreach (var category in categories.Data ?? new List<ApiCategory>())
        {
            var results = await Api.GetAsync<List<ApiRaceResult>>($"api/categories/{category.CategoryId}/results");
            foreach (var r in results.Data ?? new List<ApiRaceResult>())
            {
                byEnrolment[r.EnrolmentId] = r;
            }
        }

        return (new ResultsPageVm
        {
            Event = ev.Data,
            Enrolments = enrolments.Data ?? new(),
            ResultsByEnrolment = byEnrolment,
            Form = new ResultFormVm { EventId = eventId }
        }, null);
    }
}
