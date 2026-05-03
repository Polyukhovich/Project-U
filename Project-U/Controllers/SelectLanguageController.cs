using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Project_U.Controllers
{
    public class SelectLanguageController : Controller
    {
        private readonly IOptions<RequestLocalizationOptions> _locOptions;

        public SelectLanguageController(IOptions<RequestLocalizationOptions> locOptions)
        {
            _locOptions = locOptions;
        }

        public IActionResult Index(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var cultures = _locOptions.Value.SupportedUICultures!.ToList();
            return View(cultures);
        }

        [HttpPost]
        public IActionResult SetLanguage(string cultureName, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return string.IsNullOrEmpty(returnUrl)
                ? RedirectToAction("Index", "Home")
                : LocalRedirect(returnUrl);
        }
    }
}