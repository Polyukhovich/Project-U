using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Models;
using ProjectU.Data;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.SignalR;
using Project_U.Hubs;

namespace Controllers
{
    [Authorize]
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AssignmentsController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext )
        {
            _context = context;           
            _hubContext = hubContext;
    
        }

        // GET: Assignments — всі ролі можуть переглядати
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10;
            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .ToListAsync();
            var paged = assignments.ToPagedList(page, pageSize);
            return View(paged);
        }

        // GET: Assignments/Details/5
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.PlagiarismResults)
                        .ThenInclude(p => p.ComparedWith)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // GET: Assignments/Create — тільки Teacher та Admin
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            // Показуємо тільки курси поточного викладача
            var courses = await _context.Courses
                .Where(c => c.TeacherId == currentUser!.Id)
                .ToListAsync();

            ViewData["CourseId"] = new SelectList(courses, "Id", "Name");
            return View();
        }

        // POST: Assignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Deadline,CourseId")] Assignment assignment)
        {
            if (ModelState.IsValid)
            {
                assignment.CreatedAt = DateTime.UtcNow;
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                // Сповіщення студентам групи про нове завдання
                var course = await _context.Courses
                    .Include(c => c.Group)
                        .ThenInclude(g => g.Students)
                    .FirstOrDefaultAsync(c => c.Id == assignment.CourseId);

                if (course?.Group?.Students != null)
                {
                    var message = $"📋 Новe завдання: '{assignment.Title}' з курсу '{course.Name}'. Дедлайн: {assignment.Deadline:dd.MM.yyyy HH:mm}";

                    foreach (var student in course.Group.Students)
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
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            var courses = await _context.Courses
                .Where(c => c.TeacherId == currentUser!.Id)
                .ToListAsync();
            ViewData["CourseId"] = new SelectList(courses, "Id", "Name");
            return View(assignment);
        }

        // GET: Assignments/Edit/5 — тільки Teacher та Admin
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return NotFound();

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            var courses = await _context.Courses
                .Where(c => c.TeacherId == currentUser!.Id)
                .ToListAsync();
            ViewData["CourseId"] = new SelectList(courses, "Id", "Name", assignment.CourseId);
            return View(assignment);
        }

        // POST: Assignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Deadline,CourseId")] Assignment assignment)
        {
            if (id != assignment.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    assignment.CreatedAt = DateTime.UtcNow;
                    _context.Update(assignment);
                    await _context.SaveChangesAsync();
                    // Сповіщення студентам про зміну завдання
                    var editedCourse = await _context.Courses
                        .Include(c => c.Group)
                            .ThenInclude(g => g.Students)
                        .FirstOrDefaultAsync(c => c.Id == assignment.CourseId);

                    if (editedCourse?.Group?.Students != null)
                    {
                        var message = $"✏️ Завдання '{assignment.Title}' було оновлено. Новий дедлайн: {assignment.Deadline:dd.MM.yyyy HH:mm}";

                        foreach (var student in editedCourse.Group.Students)
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
                    if (!_context.Assignments.Any(e => e.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            var courses = await _context.Courses
                .Where(c => c.TeacherId == currentUser!.Id)
                .ToListAsync();
            ViewData["CourseId"] = new SelectList(courses, "Id", "Name", assignment.CourseId);
            return View(assignment);
        }
        // GET: Assignments/Grade/5 — оцінити здачу
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Grade(int? id)
        {
            if (id == null) return NotFound();

            var submission = await _context.LabWorks
                .Include(l => l.Student)
                .Include(l => l.Assignment)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (submission == null) return NotFound();

            return View(submission);
        }

        // POST: Assignments/Grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Grade(int labWorkId, int value)
        {
            var labWork = await _context.LabWorks
                .Include(l => l.Assignment)
                .FirstOrDefaultAsync(l => l.Id == labWorkId);

            if (labWork == null) return NotFound();

            // Зберігаємо оцінку
            var grade = new Grade
            {
                Value = value,
                StudentId = labWork.StudentId,
                CourseId = labWork.Assignment!.CourseId,
                LabWorkId = labWork.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Grades.Add(grade);

            // Позначаємо роботу як оцінену
            labWork.IsGraded = true;
            _context.Update(labWork);

            await _context.SaveChangesAsync();

            // Надсилаємо сповіщення через SignalR
            var course = await _context.Courses.FindAsync(labWork.Assignment.CourseId);
            var message = $"Вам виставлено оцінку {value} з курсу {course?.Name}";

            await _hubContext.Clients
                .Group($"user_{labWork.StudentId}")
                .SendAsync("ReceiveNotification", message);

            _context.Notifications.Add(new Notification
            {
                UserId = labWork.StudentId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = labWork.Assignment.Id });
        }
        // GET: Assignments/Delete/5 — тільки Admin та Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // POST: Assignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Сповіщення студентам про видалення завдання
            var deletedAssignment = await _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Group)
                        .ThenInclude(g => g.Students)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (deletedAssignment?.Course?.Group?.Students != null)
            {
                var message = $"❌ Завдання '{deletedAssignment.Title}' з курсу '{deletedAssignment.Course.Name}' було видалено";

                foreach (var student in deletedAssignment.Course.Group.Students)
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

            // Тепер видаляємо
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment != null)
            {
                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}