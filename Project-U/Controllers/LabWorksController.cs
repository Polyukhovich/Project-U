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
using ProjectU.Core.Services;


namespace Controllers
{
    [Authorize]
    public class LabWorksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PlagiarismService _plagiarismService;
        private readonly FileTextExtractorService _fileExtractor;
        private readonly IWebHostEnvironment _environment;

        public LabWorksController(ApplicationDbContext context,
               PlagiarismService plagiarismService, 
               FileTextExtractorService fileExtractor,
               IWebHostEnvironment environment)
        {
            _context = context;
            _plagiarismService = plagiarismService;
            _fileExtractor = fileExtractor;
            _environment = environment;
        }

        // GET: LabWorks — всі ролі можуть переглядати
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10;
            var labWorks = await _context.LabWorks
                .Include(l => l.Student)
                .Include(l => l.Course)
                .ToListAsync();
            var pagedSchedules = labWorks.ToPagedList(page, pageSize);
            return View(pagedSchedules);
        }

        // GET: LabWorks/Details/5
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var labWork = await _context.LabWorks
                .Include(l => l.Student)
                .Include(l => l.Course)
                .Include(l => l.PlagiarismResults)
                    .ThenInclude(p => p.ComparedWith)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (labWork == null) return NotFound();

            return View(labWork);
        }

        // GET: LabWorks/Create — Student та Admin
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Create(int? assignmentId = null)
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            if (currentUser == null) return NotFound();

            // Показуємо завдання для групи студента
            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .Where(a => a.Course.GroupId == currentUser.GroupId && a.Deadline > DateTime.UtcNow)
                .ToListAsync();

            ViewData["AssignmentId"] = new SelectList(assignments, "Id", "Title", assignmentId);
            ViewData["CurrentUserId"] = currentUser.Id;
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
                {
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
                return RedirectToAction(nameof(Index));
            }

            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Email", labWork.StudentId);
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Name", labWork.CourseId);
            return View(labWork);
        }

        // GET: LabWorks/Edit/5 — Student може редагувати тільки свою
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labWork = await _context.LabWorks.FindAsync(id);
            if (labWork == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Description", labWork.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Id", labWork.StudentId);
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labWork = await _context.LabWorks
                .Include(l => l.Course)
                .Include(l => l.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (labWork == null)
            {
                return NotFound();
            }

            return View(labWork);
        }

        // POST: LabWorks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var labWork = await _context.LabWorks.FindAsync(id);
            if (labWork != null)
            {
                _context.LabWorks.Remove(labWork);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LabWorkExists(int id)
        {
            return _context.LabWorks.Any(e => e.Id == id);
        }
    }
}
