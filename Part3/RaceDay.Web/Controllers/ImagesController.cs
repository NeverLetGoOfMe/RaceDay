using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Passes images through from the API, so the browser (and this app) never talks to Azure directly
public class ImagesController : Controller
{
    private readonly ApiClient _api;

    public ImagesController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string id)
    {
        var (bytes, contentType) = await _api.GetBytesAsync($"api/images/{Uri.EscapeDataString(id)}");
        if (bytes == null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=3600";
        return File(bytes, contentType);
    }
}
