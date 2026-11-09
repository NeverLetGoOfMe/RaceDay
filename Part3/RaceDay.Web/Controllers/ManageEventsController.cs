using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageEventsController : AppController
{
    public ManageEventsController(ApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<ApiEventSummary>>("api/events/mine");
        if (!result.Success)
        {
            return ApiFailure(result);
        }

        return View(result.Data ?? new List<ApiEventSummary>());
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EventFormVm { EventDate = DateTime.Today.AddDays(30) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormVm model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var created = await Api.PostAsync<ApiEvent>("api/events", ToBody(model));
        if (!created.Success || created.Data == null)
        {
            ModelState.AddModelError(string.Empty, created.Error);
            return View("Form", model);
        }

        await UploadBannerAsync(created.Data.EventId, model.Banner);
        Flash(true, "Your event has been created. Add categories next so participants can enrol.");
        return RedirectToAction("Index", "ManageCategories", new { eventId = created.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetAsync<ApiEvent>($"api/events/{id}");
        if (!result.Success || result.Data == null)
        {
            return ApiFailure(result);
        }

        if (result.Data.OrganiserId != CurrentUserId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var e = result.Data;
        return View("Form", new EventFormVm
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            RouteInfo = e.RouteInfo,
            Distance = e.Distance,
            EventType = e.EventType,
            ExistingBanner = e.BannerImage
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormVm model)
    {
        model.EventId = id;
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var updated = await Api.PutAsync<ApiEvent>($"api/events/{id}", ToBody(model));
        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error);
            return View("Form", model);
        }

        await UploadBannerAsync(id, model.Banner);
        Flash(true, "Your event has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");
        Flash(result.Success, result.Success ? "The event has been deleted." : result.Error);
        return RedirectToAction(nameof(Index));
    }

    private static object ToBody(EventFormVm m) => new
    {
        eventName = m.EventName,
        eventDate = m.EventDate!.Value.ToString("yyyy-MM-dd"),
        location = m.Location,
        description = m.Description,
        routeInfo = m.RouteInfo,
        distance = m.Distance,
        eventType = m.EventType
    };

    // The event is saved first; if the banner upload fails the organiser is told, but the event is kept
    private async Task UploadBannerAsync(int eventId, IFormFile? banner)
    {
        if (banner == null || banner.Length == 0)
        {
            return;
        }

        var upload = await Api.PostFileAsync<ApiImage>($"api/events/{eventId}/banner", banner);
        if (!upload.Success)
        {
            Flash(false, $"The event was saved, but the banner could not be uploaded: {upload.Error}");
        }
    }
}
