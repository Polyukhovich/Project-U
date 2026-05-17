using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Project_U.Helpers;
using Project_U.Hubs;
using ProjectU.Core.Models;
using ProjectU.Data;
using X.PagedList.Extensions;
using Project_U.Helpers;
using Microsoft.Extensions.Localization;

namespace Controllers
{
    [Authorize]
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IWebHostEnvironment _environment;
        private readonly NotificationHelper _notificationHelper;
        private readonly IStringLocalizer _localizer;
        private readonly AuditService _auditService;

        public AssignmentsController(ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext, 
            IWebHostEnvironment environment,
            NotificationHelper notificationHelper,
            IStringLocalizerFactory localizerFactory,
            AuditService auditService)
        {
            _context = context;
            _hubContext = hubContext;
            _environment = environment;
            _notificationHelper = notificationHelper;
            _localizer = localizerFactory.Create(
                "ModelValidation",
                typeof(Program).Assembly.GetName().Name!);
            _auditService = auditService;
        }

        // GET: Assignments — всі ролі можуть переглядати
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index(int? courseId)
        {
            var currentUser = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var assignmentsQuery = _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                .AsQueryable();

            if (User.IsInRole("Student") && currentUser?.GroupId != null)
                assignmentsQuery = assignmentsQuery
                    .Where(a => a.Course.GroupId == currentUser.GroupId);

            if (User.IsInRole("Teacher"))
                assignmentsQuery = assignmentsQuery
                    .Where(a => a.Course.TeacherId == currentUser!.Id);

            if (courseId != null)
                assignmentsQuery = assignmentsQuery.Where(a => a.CourseId == courseId);

            var assignments = await assignmentsQuery
                .OrderByDescending(a => a.Deadline)
                .ToListAsync();

            var grouped = assignments
                .GroupBy(a => a.Course?.Name ?? "—")
                .Select(g => new
                {
                    CourseName = g.Key,
                    Assignments = g.ToList(),
                    HasExpired = g.Any(a => a.Deadline < DateTime.Now),
                    HasUrgent = g.Any(a => a.Deadline > DateTime.Now &&
                                           (a.Deadline - DateTime.Now).TotalHours < 24)
                })
                .OrderBy(g => g.CourseName)
                .ToList();

            var courses = await _context.Courses.ToListAsync();
            ViewBag.GroupedAssignments = grouped;
            ViewBag.Courses = courses;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.CurrentUserId = currentUser?.Id;
            return View();
        }

        // GET: Assignments/Details/5
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Group)
                        .ThenInclude(g => g.Students)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.PlagiarismResults)
                        .ThenInclude(p => p.ComparedWith)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            // Завантажуємо оцінки для цього завдання
            var labWorkIds = assignment.Submissions.Select(s => s.Id).ToList();
            var grades = await _context.Grades
                .Where(g => g.LabWorkId != null && labWorkIds.Contains(g.LabWorkId.Value))
                .ToListAsync();

            ViewBag.CurrentUserId = currentUser?.Id;
            ViewBag.Grades = grades;
            return View(assignment);
        }

        // GET: Assignments/Create — тільки Teacher та Admin
        [HttpGet]
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
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Deadline,CourseId,MaterialType,MaterialUrl,AllowDownload")] Assignment assignment, IFormFile? materialFile)
        {
            if (ModelState.IsValid)
            {
                assignment.CreatedAt = DateTime.UtcNow;

                // Обробка файлу матеріалу
                if (assignment.MaterialType == "File" && materialFile != null && materialFile.Length > 0)
                {
                    // Перевірка розміру (максимум 20MB)
                    if (materialFile.Length > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", _localizer["FileTooLarge_Material"]);
                        var currentUserErr = await _context.Users
                            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                        var coursesErr = await _context.Courses
                            .Where(c => c.TeacherId == currentUserErr!.Id)
                            .ToListAsync();
                        ViewData["CourseId"] = new SelectList(coursesErr, "Id", "Name");
                        return View(assignment);
                    }

                    // Перевірка типу файлу
                    var allowedExtensions = new[] { ".pdf", ".docx" };
                    var extension = Path.GetExtension(materialFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("", _localizer["InvalidFileType_Material"]);
                        var currentUserErr = await _context.Users
                            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                        var coursesErr = await _context.Courses
                            .Where(c => c.TeacherId == currentUserErr!.Id)
                            .ToListAsync();
                        ViewData["CourseId"] = new SelectList(coursesErr, "Id", "Name");
                        return View(assignment);
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "materials");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(materialFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await materialFile.CopyToAsync(stream);
                    }

                    assignment.MaterialFilePath = filePath;
                    assignment.MaterialFileName = materialFile.FileName;
                }

                _context.Add(assignment);
                await _context.SaveChangesAsync();
                var currentUserFallback = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                await _auditService.LogAsync(
                    currentUserFallback!.Id,
                    "Create",
                    "Assignment",
                    assignment.Id.ToString(),
                    $"Створено завдання: {assignment.Title}");

                // Сповіщення студентам
                var course = await _context.Courses
                    .Include(c => c.Group)
                        .ThenInclude(g => g.Students)
                    .FirstOrDefaultAsync(c => c.Id == assignment.CourseId);

                if (course?.Group?.Students != null)
                {
                    foreach (var student in course.Group.Students)
                    {
                        var message = await _notificationHelper.GetLocalizedMessage(
                            student.Id, "NewAssignment",
                            assignment.Title, course.Name, assignment.Deadline.ToString("dd.MM.yyyy HH:mm"));

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
        [HttpGet]
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Deadline,CourseId,MaterialType,MaterialUrl,MaterialFilePath,MaterialFileName,AllowDownload")] Assignment assignment, IFormFile? materialFile)
        {
            if (id != assignment.Id) return NotFound();
            // Прибираємо валідацію дедлайну при редагуванні
            ModelState.Remove("Deadline");

            if (ModelState.IsValid)
            {
                try
                {
                    // Обробка нового файлу матеріалу
                    if (assignment.MaterialType == "File" && materialFile != null && materialFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "materials");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(materialFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await materialFile.CopyToAsync(stream);
                        }

                        assignment.MaterialFilePath = filePath;
                        assignment.MaterialFileName = materialFile.FileName;
                    }

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
                        foreach (var student in editedCourse.Group.Students)
                        {
                            var message = await _notificationHelper.GetLocalizedMessage(
                                student.Id, "AssignmentUpdated",
                                assignment.Title, assignment.Deadline.ToString("dd.MM.yyyy HH:mm"));
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
                        var currentUserFallback = await _context.Users
                            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                        await _auditService.LogAsync(
                            currentUserFallback!.Id,
                            "Edit",
                            "Assignment",
                            assignment.Id.ToString(),
                            $"Відредаговано завдання: {assignment.Title}");
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
        [HttpGet]
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
            var message = await _notificationHelper.GetLocalizedMessage(
                labWork.StudentId, "GradeReceived",
                value, course?.Name);

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
                "Grade",
                "Grade",
                grade.Id.ToString(),
                $"Виставлено оцінку {value} студенту");

            return RedirectToAction(nameof(Details), new { id = labWork.Assignment.Id });
        }
        // Перегляд матеріалу
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> ViewMaterial(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return NotFound();

            if (string.IsNullOrEmpty(assignment.MaterialFilePath) ||
                !System.IO.File.Exists(assignment.MaterialFilePath))
                return NotFound("Файл не знайдено");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(assignment.MaterialFilePath);
            var contentType = assignment.MaterialFileName!.EndsWith(".pdf")
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // Для перегляду без завантаження
            var encodedFileName = Uri.EscapeDataString(assignment.MaterialFileName!);
            Response.Headers.Add("Content-Disposition", $"inline; filename*=UTF-8''{encodedFileName}");
            return File(fileBytes, contentType);
        }

        // Завантаження матеріалу
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> DownloadMaterial(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null || !assignment.AllowDownload) return NotFound();

            if (string.IsNullOrEmpty(assignment.MaterialFilePath) ||
                !System.IO.File.Exists(assignment.MaterialFilePath))
                return NotFound("Файл не знайдено");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(assignment.MaterialFilePath);
            var contentType = assignment.MaterialFileName!.EndsWith(".pdf")
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return File(fileBytes, contentType, assignment.MaterialFileName);
        }
        // GET: Assignments/Delete/5 — тільки Admin та Teacher
        [HttpGet]
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
            var assignment = await _context.Assignments
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.PlagiarismResults)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment != null)
            {
                // Спочатку видаляємо результати антиплагіату
                foreach (var submission in assignment.Submissions)
                {
                    _context.PlagiarismResults.RemoveRange(submission.PlagiarismResults);
                }

                // Потім видаляємо оцінки пов'язані з роботами
                var labWorkIds = assignment.Submissions.Select(s => s.Id).ToList();
                var grades = await _context.Grades
                    .Where(g => g.LabWorkId != null && labWorkIds.Contains(g.LabWorkId.Value))
                    .ToListAsync();
                _context.Grades.RemoveRange(grades);

                // Видаляємо здачі
                _context.LabWorks.RemoveRange(assignment.Submissions);

                // Видаляємо завдання
                _context.Assignments.Remove(assignment);

                // Сповіщення студентам
                var deletedCourse = await _context.Courses
                    .Include(c => c.Group)
                        .ThenInclude(g => g.Students)
                    .FirstOrDefaultAsync(c => c.Id == assignment.CourseId);

                if (deletedCourse?.Group?.Students != null)
                {
                    foreach (var student in deletedCourse.Group.Students)
                    {
                        var message = await _notificationHelper.GetLocalizedMessage(
                            student.Id, "AssignmentDeleted",
                            assignment.Title, deletedCourse.Name);
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
                }

                await _context.SaveChangesAsync();
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                await _auditService.LogAsync(
                    currentUser!.Id,
                    "Delete",
                    "Assignment",
                    id.ToString(),
                    $"Видалено завдання: {assignment.Title}");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}