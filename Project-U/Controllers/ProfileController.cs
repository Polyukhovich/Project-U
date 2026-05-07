using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Models;
using ProjectU.Data;

namespace Project_U.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var roles = await _userManager.GetRolesAsync(user!);
            ViewBag.Role = roles.FirstOrDefault() ?? "Student";

            if (roles.Contains("Student"))
            {
                ViewBag.Grades = await _context.Grades
                    .Include(g => g.Course)
                    .Where(g => g.StudentId == user!.Id)
                    .OrderByDescending(g => g.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewBag.LabWorks = await _context.LabWorks
                    .Include(l => l.Assignment)
                    .Where(l => l.StudentId == user!.Id)
                    .OrderByDescending(l => l.UploadedAt)
                    .Take(5)
                    .ToListAsync();
            }

            if (roles.Contains("Teacher"))
            {
                ViewBag.Courses = await _context.Courses
                    .Include(c => c.Group)
                    .Where(c => c.TeacherId == user!.Id)
                    .ToListAsync();

                ViewBag.Assignments = await _context.Assignments
                    .Include(a => a.Course)
                    .Where(a => a.Course.TeacherId == user!.Id)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            if (roles.Contains("Admin"))
            {
                ViewBag.TotalUsers = await _context.Users.CountAsync();
                ViewBag.TotalCourses = await _context.Courses.CountAsync();
                ViewBag.TotalGroups = await _context.Groups.CountAsync();
                ViewBag.TotalLabWorks = await _context.LabWorks.CountAsync();
            }

            return View(user);
        }
    }
}