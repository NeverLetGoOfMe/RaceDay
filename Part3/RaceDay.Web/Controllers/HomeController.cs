using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    // Shown for any unexpected error or missing page - never shows technical details
    public IActionResult Error(int? code)
    {
        ViewBag.Code = code;
        ViewBag.Message = code == 404
            ? "We could not find that page."
            : "Something went wrong on our side. Please try again.";
        Response.StatusCode = code ?? 500;
        return View();
    }
}
