using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Participant-facing pages: browse events, see details, enrol
[RoleAuthorize]
public class EventsController : AppController
{
    public EventsController(ApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index(string? type, string? search, DateTime? from)
    {
        if (HttpContext.Session.Role() == "Organiser")
        {
            return RedirectToAction("Dashboard", "Organiser");
        }

        var fromDate = (from ?? DateTime.Today).Date;
        var query = $"api/events?from={fromDate:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(type))
        {
            query += $"&type={Uri.EscapeDataString(type)}";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search)}";
        }

        var result = await Api.GetAsync<List<ApiEvent>>(query);
        if (!result.Success)
        {
            return ApiFailure(result);
        }

        return View(new EventsIndexVm { Events = result.Data ?? new(), Type = type, Search = search, From = fromDate });
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await Api.GetAsync<ApiEvent>($"api/events/{id}");
        if (!ev.Success || ev.Data == null)
        {
            return ApiFailure(ev);
        }

        var categories = await Api.GetAsync<List<ApiCategory>>($"api/events/{id}/categories");
        if (!categories.Success)
        {
            return ApiFailure(categories);
        }

        return View(new EventDetailsVm { Event = ev.Data, Categories = categories.Data ?? new() });
    }

    [RoleAuthorize("Participant")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await Api.PostAsync<ApiEnrolment>("api/enrolments", new { categoryId });

        if (!result.Success)
        {
            Flash(false, result.Status == System.Net.HttpStatusCode.Conflict ? result.Error : $"We could not enrol you: {result.Error}");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Flash(true, "You are enrolled! You can see it under My Enrolments.");
        return RedirectToAction("My", "Enrolments");
    }
}
