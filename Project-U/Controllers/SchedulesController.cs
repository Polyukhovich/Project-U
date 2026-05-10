using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Project_U.Hubs;
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
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public SchedulesController(ApplicationDbContext context,
               IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Schedules — всі ролі можуть переглядати
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var currentUser = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var schedulesQuery = _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Group)
                .Include(s => s.Dates)
                .AsQueryable();

            // Студент бачить тільки свою групу
            if (User.IsInRole("Student") && currentUser?.GroupId != null)
            {
                schedulesQuery = schedulesQuery.Where(s => s.GroupId == currentUser.GroupId);
            }

            var schedules = await schedulesQuery.ToListAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Групуємо по днях тижня — показуємо тільки якщо є майбутні дати
            var days = new (string Name, DayOfWeek Day)[]
            {
        ("Понеділок", DayOfWeek.Monday),
        ("Вівторок", DayOfWeek.Tuesday),
        ("Середа", DayOfWeek.Wednesday),
        ("Четвер", DayOfWeek.Thursday),
        ("П'ятниця", DayOfWeek.Friday),
        ("Субота", DayOfWeek.Saturday)
            };

            var grouped = days.Select(d => new
            {
                Day = d.Name,
                DayOfWeek = d.Day,
                Schedules = schedules
                    .Where(s => s.Dates.Any(date =>
                        date.Date.DayOfWeek == d.Day &&
                        date.Date >= today))
                    .OrderBy(s => s.StartTime)
                    .ToList()
            }).ToList();

            ViewBag.GroupedSchedules = grouped;
            ViewBag.AllSchedules = schedules;
            return View();
        }
        // GET: Schedules/Create — Admin та Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create()
        {
            var courses = await _context.Courses.ToListAsync();
            var groups = await _context.Groups.ToListAsync();

            ViewData["CourseId"] = new SelectList(courses, "Id", "Name");
            ViewData["GroupId"] = new SelectList(groups, "Id", "Name");
            return View();
        }

        // POST: Schedules/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create([Bind("Id,StartTime,EndTime,Room,CourseId,GroupId")] Schedule schedule, string selectedDatesJson)
        {
            if (ModelState.IsValid)
            {
                _context.Add(schedule);
                await _context.SaveChangesAsync();

                // Зберігаємо обрані дати
                if (!string.IsNullOrEmpty(selectedDatesJson))
                {
                    var dates = selectedDatesJson.Split(',')
                        .Where(d => !string.IsNullOrEmpty(d))
                        .Select(d => new ScheduleDate
                        {
                            ScheduleId = schedule.Id,
                            Date = DateOnly.Parse(d)
                        });

                    _context.ScheduleDates.AddRange(dates);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            var courses = await _context.Courses.ToListAsync();
            var groups = await _context.Groups.ToListAsync();
            ViewData["CourseId"] = new SelectList(courses, "Id", "Name", schedule.CourseId);
            ViewData["GroupId"] = new SelectList(groups, "Id", "Name", schedule.GroupId);
            return View(schedule);
        }

        // GET: Schedules/Edit/5 — Admin та Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.Dates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null) return NotFound();

            var courses = await _context.Courses.ToListAsync();
            var groups = await _context.Groups.ToListAsync();
            ViewData["CourseId"] = new SelectList(courses, "Id", "Name", schedule.CourseId);
            ViewData["GroupId"] = new SelectList(groups, "Id", "Name", schedule.GroupId);
            return View(schedule);
        }

        // POST: Schedules/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,EndTime,Room,CourseId,GroupId")] Schedule schedule, string selectedDatesJson)
        {
            if (id != schedule.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);

                    // Видаляємо старі дати
                    var oldDates = await _context.ScheduleDates
                        .Where(d => d.ScheduleId == id)
                        .ToListAsync();
                    _context.ScheduleDates.RemoveRange(oldDates);

                    // Додаємо нові дати
                    if (!string.IsNullOrEmpty(selectedDatesJson))
                    {
                        var dates = selectedDatesJson.Split(',')
                            .Where(d => !string.IsNullOrEmpty(d))
                            .Select(d => new ScheduleDate
                            {
                                ScheduleId = schedule.Id,
                                Date = DateOnly.Parse(d)
                            });
                        _context.ScheduleDates.AddRange(dates);
                    }

                    await _context.SaveChangesAsync();

                    // Сповіщення студентам
                    var updatedSchedule = await _context.Schedules
                        .Include(s => s.Course)
                        .Include(s => s.Group)
                            .ThenInclude(g => g.Students)
                        .FirstOrDefaultAsync(s => s.Id == schedule.Id);

                    if (updatedSchedule?.Group?.Students != null)
                    {
                        var message = $"Змінено розклад: {updatedSchedule.Course?.Name} — {updatedSchedule.StartTime}";

                        foreach (var student in updatedSchedule.Group.Students)
                        {
                            await _hubContext.Clients
                                .Group($"user_{student.Id}")
                                .SendAsync("ReceiveNotification", message);

                            _context.Notifications.Add(new Notification
                            {
                                UserId = student.Id,
                                Message = message,
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Schedules.Any(e => e.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var courses = await _context.Courses.ToListAsync();
            var groups = await _context.Groups.ToListAsync();
            ViewData["CourseId"] = new SelectList(courses, "Id", "Name", schedule.CourseId);
            ViewData["GroupId"] = new SelectList(groups, "Id", "Name", schedule.GroupId);
            return View(schedule);
        }
        // GET: Schedules/All — загальний розклад для всіх
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> All()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Group)
                .Include(s => s.Dates)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            var days = new (string Name, DayOfWeek Day)[]
            {
        ("Понеділок", DayOfWeek.Monday),
        ("Вівторок", DayOfWeek.Tuesday),
        ("Середа", DayOfWeek.Wednesday),
        ("Четвер", DayOfWeek.Thursday),
        ("П'ятниця", DayOfWeek.Friday),
        ("Субота", DayOfWeek.Saturday)
            };

            var grouped = days.Select(d => new
            {
                Day = d.Name,
                DayOfWeek = d.Day,
                Schedules = schedules
                    .Where(s => s.Dates.Any(date =>
                        date.Date.DayOfWeek == d.Day &&
                        date.Date >= today))
                    .OrderBy(s => s.StartTime)
                    .ToList()
            }).ToList();

            ViewBag.GroupedSchedules = grouped;
            return View("Index");
        }

        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> ByDay(string day)
        {
            var currentUser = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var today = DateOnly.FromDateTime(DateTime.Today);

            var dayOfWeekMap = new Dictionary<string, DayOfWeek>
    {
        { "Понеділок", DayOfWeek.Monday },
        { "Вівторок", DayOfWeek.Tuesday },
        { "Середа", DayOfWeek.Wednesday },
        { "Четвер", DayOfWeek.Thursday },
        { "П'ятниця", DayOfWeek.Friday },
        { "Субота", DayOfWeek.Saturday }
    };

            if (!dayOfWeekMap.TryGetValue(day, out var dayOfWeek))
                return NotFound();

            // Завантажуємо всі розклади з датами
            var allSchedules = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Group)
                .Include(s => s.Dates)
                .ToListAsync();

            // Фільтруємо в пам'яті
            var schedules = allSchedules
                .Where(s => s.Dates.Any(d =>
                    d.Date.DayOfWeek == dayOfWeek &&
                    d.Date >= today))
                .ToList();

            // Фільтрація по ролі
            if (User.IsInRole("Student") && currentUser?.GroupId != null)
                schedules = schedules.Where(s => s.GroupId == currentUser.GroupId).ToList();

            if (User.IsInRole("Teacher"))
                schedules = schedules.Where(s => s.Course.TeacherId == currentUser!.Id).ToList();

            schedules = schedules.OrderBy(s => s.StartTime).ToList();

            ViewBag.Day = day;
            ViewBag.Schedules = schedules;
            return View();
        }

        // GET: Schedules/Delete/5 — тільки Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Group)
                .Include(s => s.Dates)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Dates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule != null)
            {
                // Спочатку видаляємо дати
                _context.ScheduleDates.RemoveRange(schedule.Dates);
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}
