using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Project_U.Hubs;
using ProjectU.Core.Models;
using ProjectU.Core.Services;
using ProjectU.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList.Extensions;


namespace Controllers
{
    [Authorize]
    public class LabWorksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PlagiarismService _plagiarismService;
        private readonly FileTextExtractorService _fileExtractor;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<NotificationHub> _hubContext;

        public LabWorksController(ApplicationDbContext context,
               PlagiarismService plagiarismService, 
               FileTextExtractorService fileExtractor,
               IWebHostEnvironment environment,
               IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _plagiarismService = plagiarismService;
            _fileExtractor = fileExtractor;
            _environment = environment;
            _hubContext = hubContext;
        }
        // GET: LabWorks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var labWork = await _context.LabWorks
                .Include(l => l.Student)
                .Include(l => l.Course)
                .Include(l => l.Assignment)
                .Include(l => l.PlagiarismResults)
                    .ThenInclude(p => p.ComparedWith)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (labWork == null) return NotFound();

            // Завантажуємо оцінку для цієї роботи
            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.LabWorkId == id);

            ViewBag.Grade = grade;
            return View(labWork);
        }

        // GET: LabWorks/Create — Student та Admin
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create(int? assignmentId)
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            ViewData["CurrentUserId"] = currentUser?.Id;

            if (assignmentId != null)
            {
                var assignment = await _context.Assignments
                    .Include(a => a.Course)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);

                if (assignment != null)
                {
                    ViewBag.AssignmentName = assignment.Title;
                    ViewBag.AssignmentId = assignmentId;
                    return View(new LabWork { AssignmentId = assignmentId.Value });
                }
            }

            // Якщо без assignmentId — показуємо список завдань
            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => a.Course.GroupId == currentUser!.GroupId)
                .ToListAsync();

            ViewBag.Assignments = new SelectList(assignments, "Id", "Title");
            return View();
        }

        // POST: LabWorks/Create — при завантаженні запускається антиплагіат
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Create([Bind("Id,Title,StudentId,CourseId,AssignmentId")] LabWork labWork, IFormFile? uploadedFile)
        {
            if (ModelState.IsValid)

            {    // Отримуємо CourseId з завдання
                var assignment = await _context.Assignments.FindAsync(labWork.AssignmentId);
                if (assignment != null)
                    labWork.CourseId = assignment.CourseId;
                // Обробка завантаженого файлу
                if (uploadedFile != null && uploadedFile.Length > 0)
                {    // Перевірка розміру (максимум 10MB)
                    if (uploadedFile.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Файл занадто великий. Максимальний розмір — 10MB");
                        return View(labWork);
                    }
                    var allowedExtensions = new[] { ".docx", ".pdf" };
                    var extension = Path.GetExtension(uploadedFile.FileName).ToLower();

                    if (!allowedExtensions.Any(e => e == extension))
                    {
                        ModelState.AddModelError("", "Дозволені лише файли .docx та .pdf");
                        ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Email", labWork.StudentId);
                        ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Name", labWork.CourseId);
                        return View(labWork);
                    }

                    // Зберігаємо файл в папку uploads
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "labworks");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(stream);
                    }

                    labWork.FilePath = filePath;
                    labWork.FileName = uploadedFile.FileName;

                    // Витягуємо текст з файлу для антиплагіату
                    labWork.Content = _fileExtractor.ExtractText(filePath);
                }

                labWork.UploadedAt = DateTime.UtcNow;
                _context.Add(labWork);
                await _context.SaveChangesAsync();

                // Автоматична перевірка на плагіат
                var otherWorks = await _context.LabWorks
                    .Where(l => l.CourseId == labWork.CourseId
                             && l.StudentId != labWork.StudentId
                             && l.Id != labWork.Id)
                    .ToListAsync();

                foreach (var otherWork in otherWorks)
                {
                    var similarity = _plagiarismService.CompareTexts(
                        labWork.Content, otherWork.Content);

                    var result = new PlagiarismResult
                    {
                        LabWorkId = labWork.Id,
                        ComparedWithId = otherWork.Id,
                        SimilarityPercent = similarity,
                        CheckedAt = DateTime.UtcNow
                    };

                    _context.PlagiarismResults.Add(result);
                }

                await _context.SaveChangesAsync();
                // Сповіщення студенту про результат антиплагіату
                if (otherWorks.Any())
                {
                    var maxSimilarity = otherWorks
                        .Select(w => _plagiarismService.CompareTexts(labWork.Content, w.Content))
                        .Max();

                    string plagMessage;
                    if (maxSimilarity > 50)
                        plagMessage = $"⚠️ Висока схожість ({maxSimilarity:F1}%) виявлена у вашій роботі '{labWork.Title}'";
                    else if (maxSimilarity > 20)
                        plagMessage = $"⚡ Середня схожість ({maxSimilarity:F1}%) виявлена у вашій роботі '{labWork.Title}'";
                    else
                        plagMessage = $"✅ Робота '{labWork.Title}' пройшла перевірку ({maxSimilarity:F1}% схожості)";

                    await _hubContext.Clients
                        .Group($"user_{labWork.StudentId}")
                        .SendAsync("ReceiveNotification", plagMessage);

                    _context.Notifications.Add(new Notification
                    {
                        UserId = labWork.StudentId,
                        Message = plagMessage,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
                // Сповіщення викладачу про здачу роботи
                var submittedAssignment = await _context.Assignments
                    .Include(a => a.Course)
                    .FirstOrDefaultAsync(a => a.Id == labWork.AssignmentId);

                if (submittedAssignment?.Course != null)
                {
                    var teacher = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == submittedAssignment.Course.TeacherId);

                    if (teacher != null)
                    {
                        var student = await _context.Users.FindAsync(labWork.StudentId);
                        var message = $"Студент {student?.Email} здав роботу '{labWork.Title}' до завдання '{submittedAssignment.Title}'";

                        await _hubContext.Clients
                            .Group($"user_{teacher.Id}")
                            .SendAsync("ReceiveNotification", message);

                        _context.Notifications.Add(new Notification
                        {
                            UserId = teacher.Id,
                            Message = message,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Index", "Assignments");
            }

            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Email", labWork.StudentId);
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Name", labWork.CourseId);
            return View(labWork);
        }

        // GET: LabWorks/Edit/5 — Student може редагувати тільки свою
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var labWork = await _context.LabWorks
                .Include(l => l.Student)
                .Include(l => l.Course)
                .Include(l => l.Assignment)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (labWork == null) return NotFound();

            return View(labWork);
        }

        // POST: LabWorks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,UploadedAt,StudentId,CourseId")] LabWork labWork)
        {
            if (id != labWork.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(labWork);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LabWorkExists(labWork.Id))
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Description", labWork.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Id", labWork.StudentId);
            return View(labWork);
        }

        // GET: LabWorks/Delete/5 — тільки Admin
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var labWork = await _context.LabWorks
                .Include(l => l.Student)
                .Include(l => l.Course)
                .Include(l => l.Assignment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (labWork == null) return NotFound();

            return View(labWork);
        }

        // POST: LabWorks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var labWork = await _context.LabWorks.FindAsync(id);
            if (labWork == null) return NotFound();

            // Студент може видалити тільки свою роботу
            if (User.IsInRole("Student"))
            {
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                if (labWork.StudentId != currentUser?.Id)
                    return Forbid();
            }

            // Видаляємо оцінку якщо є
            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.LabWorkId == id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
            }

            // Видаляємо результати антиплагіату
            var plagiarismResults = await _context.PlagiarismResults
                .Where(p => p.LabWorkId == id)
                .ToListAsync();
            _context.PlagiarismResults.RemoveRange(plagiarismResults);

            _context.LabWorks.Remove(labWork);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Assignments");
        }

        private bool LabWorkExists(int id)
        {
            return _context.LabWorks.Any(e => e.Id == id);
        }
    }
}
