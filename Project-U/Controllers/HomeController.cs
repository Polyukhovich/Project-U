using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Project_U.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(IStringLocalizer<HomeController> localizer)
        {
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            ViewBag.WelcomeMessage = _localizer["WelcomeMessage"];
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}