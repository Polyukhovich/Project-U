using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Models;
using ProjectU.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList.Extensions;

namespace Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager )
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Courses — всі ролі можуть переглядати
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index(string? search)
        {
            var currentUser = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var coursesQuery = _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Group)
                .Include(c => c.CourseTeachers)
                .ThenInclude(ct => ct.Teacher)
                .AsQueryable();

            // Студент бачить тільки курси своєї групи
            if (User.IsInRole("Student") && currentUser?.GroupId != null)
            {
                coursesQuery = coursesQuery.Where(c => c.GroupId == currentUser.GroupId);
            }

            // Викладач бачить тільки свої курси
            if (User.IsInRole("Teacher"))
            {
                coursesQuery = coursesQuery.Where(c => c.TeacherId == currentUser!.Id);
            }

            var courses = await coursesQuery.ToListAsync();
            // Пошук в пам'яті
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                courses = courses.Where(c =>
                    c.Name.ToLower().Contains(searchLower) ||
                    (c.Teacher != null && $"{c.Teacher.FirstName} {c.Teacher.LastName}".ToLower().Contains(searchLower)) ||
                    (c.Group != null && c.Group.Name.ToLower().Contains(searchLower)))
                    .ToList();
            }
            // Групуємо по групах
            var grouped = courses
                .GroupBy(c => c.Group?.Name ?? "—")
                .Select(g => new
                {
                    GroupName = g.Key,
                    Courses = g.ToList()
                })
                .OrderBy(g => g.GroupName)
                .ToList();

            ViewBag.GroupedCourses = grouped;
            ViewBag.Search = search;
            return View();
        }

        // GET: Courses/Details/5
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Group)
                .Include(c => c.CourseTeachers)
                    .ThenInclude(ct => ct.Teacher)
                .Include(c => c.LabWorks)
                    .ThenInclude(l => l.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // GET: Courses/Create — Admin та Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
            ViewBag.AllTeachers = teachers;
            ViewBag.CurrentUserId = currentUser?.Id;
            ViewBag.CurrentUserName = $"{currentUser?.FirstName} {currentUser?.LastName}";
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name");
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create([Bind("Id,Name,TeacherId,GroupId,CourseType")] Course course, List<string>? additionalTeacherIds)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();

                // Додаємо додаткових викладачів
                if (additionalTeacherIds != null)
                {
                    foreach (var teacherId in additionalTeacherIds)
                    {
                        _context.CourseTeachers.Add(new CourseTeacher
                        {
                            CourseId = course.Id,
                            TeacherId = teacherId,
                            Role = "Assistant"
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
            ViewBag.AllTeachers = teachers;
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name");
            return View(course);
        }

        // GET: Courses/Edit/5 — Admin та Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.CourseTeachers)
                    .ThenInclude(ct => ct.Teacher)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            // Список всіх викладачів
            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
            ViewBag.AllTeachers = teachers;
            ViewBag.CurrentUserName = $"{currentUser?.FirstName} {currentUser?.LastName}";
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", course.GroupId);
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TeacherId,GroupId,CourseType")] Course course, List<string>? additionalTeacherIds)
        {
            if (id != course.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);

                    // Видаляємо старих додаткових викладачів
                    var oldTeachers = await _context.CourseTeachers
                        .Where(ct => ct.CourseId == id)
                        .ToListAsync();
                    _context.CourseTeachers.RemoveRange(oldTeachers);

                    // Додаємо нових
                    if (additionalTeacherIds != null)
                    {
                        foreach (var teacherId in additionalTeacherIds)
                        {
                            _context.CourseTeachers.Add(new CourseTeacher
                            {
                                CourseId = id,
                                TeacherId = teacherId,
                                Role = "Assistant"
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Courses.Any(e => e.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
            ViewBag.AllTeachers = teachers;
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", course.GroupId);
            return View(course);
        }

        // GET: Courses/Delete/5 — тільки Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Group)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasLabWorks = await _context.LabWorks.AnyAsync(l => l.CourseId == id);
            var hasGrades = await _context.Grades.AnyAsync(g => g.CourseId == id);

            if (hasLabWorks || hasGrades)
            {
                var course = await _context.Courses
                    .Include(c => c.Teacher)
                    .Include(c => c.Group)
                    .FirstOrDefaultAsync(c => c.Id == id);

                ViewBag.CanDelete = false;
                return View(course);
            }

            var courseToDelete = await _context.Courses.FindAsync(id);
            if (courseToDelete != null)
            {
                _context.Courses.Remove(courseToDelete);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
