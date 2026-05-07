using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectU.Core.Models;
using ProjectU.Data;

namespace Project_U.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly ApplicationDbContext _context;

        public HomeController(IStringLocalizer<HomeController> localizer, ApplicationDbContext context)
        {
            _localizer = localizer;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Отримуємо поточного користувача
            var currentUser = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            // Розклад на сьогодні для групи студента
            var todaySchedules = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Group)
                .Include(s => s.Dates)
                .Where(s => s.GroupId == currentUser!.GroupId &&
                            s.Dates.Any(d => d.Date == today))
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.TodaySchedules = todaySchedules;
            ViewBag.Today = DateTime.Today.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("uk-UA"));

            return View();
        }
    }
}