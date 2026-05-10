using Microsoft.AspNetCore.Authorization;
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
using Microsoft.AspNetCore.SignalR;
using Project_U.Hubs;

namespace Controllers
{
    [Authorize]
    public class GradesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public GradesController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Grades — Student бачить тільки свої, Teacher/Admin бачать всі
       [Authorize(Roles = "Admin,Teacher,Student")]
public async Task<IActionResult> Index(int page = 1)
{
    var currentUser = await _context.Users
        .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

    var gradesQuery = _context.Grades
        .Include(g => g.Student)
        .Include(g => g.Course)
        .Include(g => g.LabWork)
        .AsQueryable();

    // Студент бачить тільки свої оцінки
    if (User.IsInRole("Student"))
    {
        gradesQuery = gradesQuery.Where(g => g.StudentId == currentUser!.Id);
    }

    var grades = await gradesQuery
        .OrderByDescending(g => g.CreatedAt)
        .ToListAsync();

    // Групуємо по курсах
    var grouped = grades
        .GroupBy(g => g.Course?.Name ?? "—")
        .Select(g => new
        {
            CourseName = g.Key,
            Grades = g.ToList(),
            Average = g.Average(x => x.Value)
        })
        .OrderBy(g => g.CourseName)
        .ToList();

    ViewBag.GroupedGrades = grouped;
    return View();
}

        // GET: Grades/Create — тільки Teacher та Admin
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
            }
            return RedirectToAction(nameof(Index));
        }

        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.Id == id);
        }
    }
}
