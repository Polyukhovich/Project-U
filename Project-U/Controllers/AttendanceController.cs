using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_U.Helpers;
using ProjectU.Core.Models;
using ProjectU.Data;

namespace Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public AttendanceController(ApplicationDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: Attendance/Index — перегляд відвідуваності по дисципліні
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index(int? courseId, DateOnly? dateFrom, DateOnly? dateTo, int? groupId, int page = 1)
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var attendanceQuery = _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Schedule)
                    .ThenInclude(s => s.Course)
                .Include(a => a.Schedule)
                    .ThenInclude(s => s.Group)
                .AsQueryable();

            // Студент бачить відвідуваність своєї групи
            if (User.IsInRole("Student") && currentUser?.GroupId != null)
            {
                var userGroup = await _context.Groups
                    .Include(g => g.Students)
                    .FirstOrDefaultAsync(g => g.Id == currentUser.GroupId);

                var groupStudentIds = userGroup?.Students.Select(s => s.Id).ToList() ?? new List<string>();
                attendanceQuery = attendanceQuery
                    .Where(a => groupStudentIds.Contains(a.StudentId));
            }

            // Викладач бачить тільки свої дисципліни
            if (User.IsInRole("Teacher"))
                attendanceQuery = attendanceQuery
                    .Where(a => a.Schedule.Course.TeacherId == currentUser!.Id);

            // Фільтри
            if (courseId != null)
                attendanceQuery = attendanceQuery
                    .Where(a => a.Schedule.CourseId == courseId);

            if (dateFrom != null)
                attendanceQuery = attendanceQuery
                    .Where(a => a.Date >= dateFrom);

            if (dateTo != null)
                attendanceQuery = attendanceQuery
                    .Where(a => a.Date <= dateTo);

            if (groupId != null)
                attendanceQuery = attendanceQuery
                    .Where(a => a.Schedule.GroupId == groupId);

            var attendance = await attendanceQuery
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            // Групуємо по занятті (дата + розклад)
            var grouped = attendance
                .GroupBy(a => new { a.ScheduleId, a.Date })
                .Select(g => new
                {
                    ScheduleId = g.Key.ScheduleId,
                    Date = g.Key.Date,
                    CourseName = g.First().Schedule?.Course?.Name ?? "—",
                    GroupName = g.First().Schedule?.Group?.Name ?? "—",
                    PresentCount = g.Count(a => a.IsPresent),
                    AbsentCount = g.Count(a => !a.IsPresent),
                    Students = g.ToList()
                })
                .OrderByDescending(g => g.Date)
                .ToList();

            // Пагінація
            int pageSize = 10;
            int totalItems = grouped.Count;
            var pagedGroups = grouped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Списки для фільтрів
            var courses = await _context.Courses.ToListAsync();
            var groups = await _context.Groups.ToListAsync();

            ViewBag.Courses = courses;
            ViewBag.Groups = groups;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.SelectedGroupId = groupId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.GroupedAttendance = pagedGroups;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            return View();
        }

        // GET: Attendance/Mark — відмітити відвідуваність
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Mark(int scheduleId, DateOnly date)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Group)
                    .ThenInclude(g => g.Students)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null) return NotFound();

            // Отримуємо існуючі записи відвідуваності
            var existing = await _context.Attendances
                .Where(a => a.ScheduleId == scheduleId && a.Date == date)
                .ToListAsync();

            ViewBag.Schedule = schedule;
            ViewBag.Date = date;
            ViewBag.Existing = existing;
            return View();
        }

        // POST: Attendance/Mark
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Mark(int scheduleId, DateOnly date,
            List<string> studentIds, List<string> presentIds)
        {
            // Видаляємо старі записи для цього заняття
            var existing = await _context.Attendances
                .Where(a => a.ScheduleId == scheduleId && a.Date == date)
                .ToListAsync();
            _context.Attendances.RemoveRange(existing);

            // Додаємо нові записи
            foreach (var studentId in studentIds)
            {
                _context.Attendances.Add(new Attendance
                {
                    StudentId = studentId,
                    ScheduleId = scheduleId,
                    Date = date,
                    IsPresent = presentIds.Contains(studentId),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            await _auditService.LogAsync(
                currentUser!.Id,
                "Mark",
                "Attendance",
                scheduleId.ToString(),
                $"Відмічено відвідуваність на {date}");

            return RedirectToAction(nameof(Index));
        }
    }
}