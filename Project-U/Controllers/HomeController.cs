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
            // Отримуємо поточного користувача
            var currentUser = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            // Розклад на сьогодні
            var today = DateOnly.FromDateTime(DateTime.Today);

            var schedulesQuery = _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Group)
                .Include(s => s.Dates)
                .Where(s => s.Dates.Any(d => d.Date == today));

            // Студент бачить тільки свою групу
            if (User.IsInRole("Student") && currentUser?.GroupId != null)
            {
                schedulesQuery = schedulesQuery.Where(s => s.GroupId == currentUser.GroupId);
            }

            // Викладач бачить тільки свої пари
            if (User.IsInRole("Teacher"))
            {
                schedulesQuery = schedulesQuery.Where(s => s.Course.TeacherId == currentUser!.Id);
            }

            var todaySchedules = await schedulesQuery
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.TodaySchedules = todaySchedules;
            ViewBag.Today = DateTime.Today.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("uk-UA"));

            return View();
        }

        [AllowAnonymous]
        public IActionResult Error(int code)
        {
            ViewBag.Code = code;
            return View();
        }
    }
}