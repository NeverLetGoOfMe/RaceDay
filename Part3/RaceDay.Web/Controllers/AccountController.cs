using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    public AccountController(ApiClient api) : base(api)
    {
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.UserId() != null)
        {
            return RedirectToHome();
        }

        return View(new LoginVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var login = await Api.PostAsync<ApiLogin>("api/auth/login", new { email = model.Email, password = model.Password });
        if (!login.Success || login.Data == null)
        {
            ModelState.AddModelError(string.Empty, login.Error);
            return View(model);
        }

        var me = await Api.GetAsync<ApiUser>("api/users/me");

        HttpContext.Session.SetInt32("UserId", login.Data.UserId);
        HttpContext.Session.SetString("Role", login.Data.Role);
        HttpContext.Session.SetString("FullName", me.Data?.FullName ?? model.Email);
        HttpContext.Session.SetString("ProfilePicture", me.Data?.ProfilePicture ?? string.Empty);

        return RedirectToHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.UserId() != null)
        {
            return RedirectToHome();
        }

        return View(new RegisterVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await Api.PostAsync<ApiUser>("api/auth/register", new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            contactNumber = model.ContactNumber,
            role = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        Flash(true, "Your account has been created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await Api.PostAsync<object>("api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome() =>
        HttpContext.Session.Role() == "Organiser"
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Events");
}
