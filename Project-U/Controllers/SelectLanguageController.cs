using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectU.Core.Models;
using ProjectU.Data;

namespace Project_U.Controllers
{
    public class SelectLanguageController : Controller
    {
        private readonly IOptions<RequestLocalizationOptions> _locOptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SelectLanguageController(
            IOptions<RequestLocalizationOptions> locOptions,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _locOptions = locOptions;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var cultures = _locOptions.Value.SupportedUICultures!.ToList();
            return View(cultures);
        }

        [HttpPost]
        public async Task<IActionResult> SetLanguage(string cultureName, string returnUrl)
        {
            // Зберігаємо мову в cookie
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Зберігаємо мову в БД якщо користувач залогінений
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    user.PreferredLanguage = cultureName;
                    await _userManager.UpdateAsync(user);
                }
            }

            return string.IsNullOrEmpty(returnUrl)
                ? RedirectToAction("Index", "Home")
                : LocalRedirect(returnUrl);
        }
    }
}