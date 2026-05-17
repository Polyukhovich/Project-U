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
                .Include(a => a.SubTasks)
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
                // Зберігаємо підзавдання
                var subTaskTitles = Request.Form["SubTaskTitles"].ToList();
                var subTaskMaxScores = Request.Form["SubTaskMaxScores"].ToList();

                for (int i = 0; i < subTaskTitles.Count; i++)
                {
                    if (!string.IsNullOrEmpty(subTaskTitles[i]))
                    {
                        _context.SubTasks.Add(new SubTask
                        {
                            Title = subTaskTitles[i],
                            MaxScore = int.TryParse(subTaskMaxScores.ElementAtOrDefault(i), out var score) ? score : 10,
                            AssignmentId = assignment.Id
                        });
                    }
                }
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
                    .ThenInclude(a => a.SubTasks)
                .Include(l => l.SubTaskGrades)
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
                    .ThenInclude(a => a.SubTasks)
                .FirstOrDefaultAsync(l => l.Id == labWorkId);

            if (labWork == null) return NotFound();

            // Перевіряємо чи є підзавдання
            var subTaskIds = Request.Form["subTaskIds"].ToList();
            var subTaskValues = Request.Form["subTaskValues"].ToList();

            int finalValue;

            if (subTaskIds.Any() && labWork.Assignment?.SubTasks != null && labWork.Assignment.SubTasks.Any())
            {
                // Спочатку перевіряємо валідацію
                for (int i = 0; i < subTaskIds.Count; i++)
                {
                    var subTaskId = int.Parse(subTaskIds[i]);
                    var subTask = labWork.Assignment?.SubTasks?.FirstOrDefault(s => s.Id == subTaskId);
                    var subTaskValue = int.TryParse(subTaskValues.ElementAtOrDefault(i), out var v) ? v : 0;

                    if (subTask != null && subTaskValue > subTask.MaxScore)
                    {
                        ModelState.AddModelError("", $"Бал за підзавдання '{subTask.Title}' не може перевищувати {subTask.MaxScore}");
                        var submissionErr = await _context.LabWorks
                            .Include(l => l.Student)
                            .Include(l => l.Assignment)
                                .ThenInclude(a => a.SubTasks)
                            .Include(l => l.SubTaskGrades)
                            .FirstOrDefaultAsync(l => l.Id == labWorkId);
                        return View(submissionErr);
                    }
                }

                // Перевірка суми
                int total = subTaskValues
                    .Select(s => int.TryParse(s, out var v) ? v : 0)
                    .Sum();

                if (total > 100)
                {
                    ModelState.AddModelError("", "Сума балів не може перевищувати 100");
                    var submissionErr = await _context.LabWorks
                        .Include(l => l.Student)
                        .Include(l => l.Assignment)
                            .ThenInclude(a => a.SubTasks)
                        .Include(l => l.SubTaskGrades)
                        .FirstOrDefaultAsync(l => l.Id == labWorkId);
                    return View(submissionErr);
                }

                // Видаляємо старі оцінки за підзавдання
                var oldSubTaskGrades = await _context.SubTaskGrades
                    .Where(g => g.LabWorkId == labWorkId)
                    .ToListAsync();
                _context.SubTaskGrades.RemoveRange(oldSubTaskGrades);

                // Зберігаємо нові оцінки за підзавдання
                for (int i = 0; i < subTaskIds.Count; i++)
                {
                    var subTaskId = int.Parse(subTaskIds[i]);
                    var subTaskValue = int.TryParse(subTaskValues.ElementAtOrDefault(i), out var v2) ? v2 : 0;

                    _context.SubTaskGrades.Add(new SubTaskGrade
                    {
                        SubTaskId = subTaskId,
                        LabWorkId = labWorkId,
                        Value = subTaskValue
                    });
                }

                finalValue = Math.Min(total, 100);
            }
            else
            {
                finalValue = value;
            }

            // Зберігаємо підсумкову оцінку
            var grade = new Grade
            {
                Value = finalValue,
                StudentId = labWork.StudentId,
                CourseId = labWork.Assignment!.CourseId,
                LabWorkId = labWork.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Grades.Add(grade);
            labWork.IsGraded = true;
            _context.Update(labWork);
            await _context.SaveChangesAsync();

            // Надсилаємо сповіщення
            var course = await _context.Courses.FindAsync(labWork.Assignment.CourseId);
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
                "Grade",
                "Grade",
                grade.Id.ToString(),
                $"Виставлено оцінку {finalValue} студенту");

            return RedirectToAction(nameof(Details), new { id = labWork.Assignment.Id });
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