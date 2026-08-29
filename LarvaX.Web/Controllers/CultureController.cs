using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers;

public class CultureController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(culture) && (culture == "en" || culture == "bn"))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
        }

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? Url.Content("~/")! : returnUrl);
    }
}
