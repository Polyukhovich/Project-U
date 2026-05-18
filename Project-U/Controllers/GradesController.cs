using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Project_U.Helpers;
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
    public class GradesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AuditService _auditService;
        private readonly NotificationHelper _notificationHelper;

        public GradesController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, AuditService auditService, NotificationHelper notificationHelper)
        {
            _context = context;
            _hubContext = hubContext;
            _auditService = auditService;
            _notificationHelper = notificationHelper;
        }

        // GET: Grades — Student бачить тільки свої, Teacher/Admin бачать всі
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index(int? courseId)
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var gradesQuery = _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Course)
                .Include(g => g.LabWork)
                .AsQueryable();

            if (User.IsInRole("Student"))
                gradesQuery = gradesQuery.Where(g => g.StudentId == currentUser!.Id);

            // Фільтр по курсу
            if (courseId != null)
                gradesQuery = gradesQuery.Where(g => g.CourseId == courseId);

            var grades = await gradesQuery
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            var grouped = grades
                .GroupBy(g => g.Course?.Name ?? "—")
                .Select(g => new
                {
                    CourseName = g.Key,
                    Grades = g.ToList(),
                    Average = g.Average(x => x.Value),
                    CourseType = g.First().Course?.CourseType ?? ProjectU.Core.Models.CourseType.Exam
                })
                .OrderBy(g => g.CourseName)
                .ToList();

            // Список курсів для фільтру
            var courses = await _context.Courses.ToListAsync();
            ViewBag.Courses = courses;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.GroupedGrades = grouped;
            return View();
        }


        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Course)
                .Include(g => g.LabWork)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (grade == null) return NotFound();

            return View(grade);
        }
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DirectGrade(string studentId, int assignmentId)
        {
            var student = await _context.Users.FindAsync(studentId);
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.SubTasks)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (student == null || assignment == null) return NotFound();

            ViewBag.StudentName = $"{student.FirstName} {student.LastName}";
            ViewBag.AssignmentName = assignment.Title;
            ViewBag.CourseName = assignment.Course?.Name;

            var labWork = new LabWork
            {
                StudentId = studentId,
                AssignmentId = assignmentId,
                CourseId = assignment.CourseId,
                Title = $"Оцінка без здачі — {student.FirstName} {student.LastName}",
                UploadedAt = DateTime.UtcNow
            };

            return View(labWork);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DirectGrade(LabWork labWork, int value,
            List<int>? subTaskIds, List<int>? subTaskValues)
        {
            var assignment = await _context.Assignments
                .Include(a => a.SubTasks)
                .FirstOrDefaultAsync(a => a.Id == labWork.AssignmentId);

            if (assignment == null) return NotFound();

            labWork.CourseId = assignment.CourseId;
            labWork.UploadedAt = DateTime.UtcNow;
            labWork.IsGraded = true;

            _context.LabWorks.Add(labWork);
            await _context.SaveChangesAsync();

            int finalValue = value;

            if (subTaskIds != null && subTaskIds.Any() && assignment.SubTasks.Any())
            {
                int total = 0;
                for (int i = 0; i < subTaskIds.Count; i++)
                {
                    var subTaskValue = subTaskValues?.ElementAtOrDefault(i) ?? 0;
                    _context.SubTaskGrades.Add(new SubTaskGrade
                    {
                        SubTaskId = subTaskIds[i],
                        LabWorkId = labWork.Id,
                        Value = subTaskValue
                    });
                    total += subTaskValue;
                }
                finalValue = Math.Min(total, 100);
                await _context.SaveChangesAsync();
            }

            var grade = new Grade
            {
                Value = finalValue,
                StudentId = labWork.StudentId,
                CourseId = labWork.CourseId,
                LabWorkId = labWork.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();

            // Сповіщення студенту
            var course = await _context.Courses.FindAsync(labWork.CourseId);
            var message = await _notificationHelper.GetLocalizedMessage(
                labWork.StudentId, "GradeReceived",
                finalValue, course?.Name);

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

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            await _auditService.LogAsync(
                currentUser!.Id,
                "DirectGrade",
                "Grade",
                grade.Id.ToString(),
                $"Виставлено оцінку {finalValue} без здачі роботи");

            return RedirectToAction(nameof(Details), "Assignments", new { id = labWork.AssignmentId });
        }
        // GET: Grades/Edit/5 — тільки Teacher та Admin
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Course)
                .Include(g => g.LabWork)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grade == null) return NotFound();

            return View(grade);
        }

        // POST: Grades/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Value,CreatedAt,StudentId,CourseId,LabWorkId")] Grade grade)
        {
            if (id != grade.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grade);
                    await _context.SaveChangesAsync();
                    var currentUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                    await _auditService.LogAsync(
                        currentUser!.Id,
                        "Edit",
                        "Grade",
                        grade.Id.ToString(),
                        $"Відредаговано оцінку: {grade.Value}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GradeExists(grade.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Description", grade.CourseId);
            ViewData["LabWorkId"] = new SelectList(_context.LabWorks, "Id", "Content", grade.LabWorkId);
            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Id", grade.StudentId);
            return View(grade);
        }

        // GET: Grades/Delete/5 — тільки Admin та Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Course)
                .Include(g => g.LabWork)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (grade == null) return NotFound();

            return View(grade);
        }

        // POST: Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
            {
                // Якщо є LabWork — змінюємо статус на неоцінений
                if (grade.LabWorkId != null)
                {
                    var labWork = await _context.LabWorks.FindAsync(grade.LabWorkId);
                    if (labWork != null)
                    {
                        labWork.IsGraded = false;
                        _context.Update(labWork);
                    }
                }

                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                await _auditService.LogAsync(
                    currentUser!.Id,
                    "Delete",
                    "Grade",
                    id.ToString(),
                    $"Видалено оцінку");
            }
            return RedirectToAction(nameof(Index));
        }

        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.Id == id);
        }
    }
}
