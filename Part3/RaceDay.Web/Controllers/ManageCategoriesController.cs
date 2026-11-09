using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class ManageCategoriesController : AppController
{
    public ManageCategoriesController(ApiClient api) : base(api)
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
    public async Task<IActionResult> Create(int eventId, CategoryFormVm form)
    {
        if (!ModelState.IsValid)
        {
            var page = await LoadAsync(eventId);
            if (page.Failure != null)
            {
                return page.Failure;
            }

            page.Model!.Form = form;
            return View("Index", page.Model);
        }

        var result = await Api.PostAsync<ApiCategory>($"api/events/{eventId}/categories", new
        {
            categoryName = form.CategoryName,
            maxParticipants = form.MaxParticipants,
            entryFee = form.EntryFee
        });

        Flash(result.Success, result.Success ? "Category added." : result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        Flash(result.Success, result.Success ? "Category removed." : result.Error);
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<(CategoriesPageVm? Model, IActionResult? Failure)> LoadAsync(int eventId)
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

        var categories = await Api.GetAsync<List<ApiCategory>>($"api/events/{eventId}/categories");
        if (!categories.Success)
        {
            return (null, ApiFailure(categories));
        }

        return (new CategoriesPageVm { Event = ev.Data, Categories = categories.Data ?? new(), Form = new CategoryFormVm { EventId = eventId } }, null);
    }
}
