using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize]
public class ProfileController : AppController
{
    public ProfileController(ApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<ApiUser>("api/users/me");
        if (!result.Success || result.Data == null)
        {
            return ApiFailure(result);
        }

        return View(ToVm(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await Api.PutAsync<ApiUser>("api/users/me", new { fullName = model.FullName, contactNumber = model.ContactNumber });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        HttpContext.Session.SetString("FullName", model.FullName);
        Flash(true, "Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            Flash(false, "Please choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.PostFileAsync<ApiImage>("api/users/me/picture", picture);
        if (!result.Success)
        {
            Flash(false, result.Error);
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetString("ProfilePicture", result.Data?.Image ?? string.Empty);
        Flash(true, "Your profile picture has been updated.");
        return RedirectToAction(nameof(Index));
    }

    private static ProfileVm ToVm(ApiUser user) => new()
    {
        FullName = user.FullName,
        ContactNumber = user.ContactNumber,
        Email = user.Email,
        Role = user.Role,
        ProfilePicture = user.ProfilePicture
    };
}
