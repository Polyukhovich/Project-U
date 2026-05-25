using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Project_U.Models;
using ProjectU.Core.Models;
using ProjectU.Data;

namespace Project_U.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin — список всіх користувачів
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10;
            var originalSearch = search;

            var usersQuery = _userManager.Users
                .Include(u => u.Group)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.Email!.ToLower().Contains(searchLower));
            }

            var totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery
                .OrderBy(u => u.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            ViewBag.Search = originalSearch;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.TotalUsers = totalUsers;

            var allUsers = await _userManager.Users.ToListAsync();
            var allRoles = new Dictionary<string, IList<string>>();
            foreach (var u in allUsers)
                allRoles[u.Id] = await _userManager.GetRolesAsync(u);

            ViewBag.TotalStudents = allRoles.Values.Count(r => r.Contains("Student"));
            ViewBag.TotalTeachers = allRoles.Values.Count(r => r.Contains("Teacher"));
            return View(users);
        }

        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name");
            ViewData["Roles"] = new SelectList(new[] { "Student", "Teacher", "Admin" });
            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    GroupId = model.GroupId,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", model.GroupId);
            ViewData["Roles"] = new SelectList(new[] { "Student", "Teacher", "Admin" }, model.Role);
            return View(model);
        }

        // GET: Admin/EditUser/userId
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = currentRoles.FirstOrDefault();
            ViewData["Roles"] = new SelectList(new[] { "Student", "Teacher", "Admin" }, currentRoles.FirstOrDefault());
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", user.GroupId);
            return View(user);
        }

        // POST: Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string email, string role, int? groupId, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Оновлюємо дані
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.UserName = email;
            user.GroupId = groupId;

            await _userManager.UpdateAsync(user);

            // Зміна пароля якщо введено
            if (!string.IsNullOrEmpty(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, newPassword);
            }
            // Оновлюємо роль
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditLog(string? search, string? actionFilter, int page = 1)
        {
            int pageSize = 20;

            var query = _context.AuditLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(l =>
                    (l.User != null && (l.User.FirstName.ToLower().Contains(s) ||
                     l.User.LastName.ToLower().Contains(s))) ||
                    (l.Details != null && l.Details.ToLower().Contains(s)) ||
                    (l.EntityType != null && l.EntityType.ToLower().Contains(s)));
            }

            if (!string.IsNullOrEmpty(actionFilter))
                query = query.Where(l => l.Action == actionFilter);

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Action = actionFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total = total;
            return View(logs);
        }
        // GET: Admin/DeleteUser
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = roles.FirstOrDefault() ?? "—";
            return View(user);
        } 

        // POST: Admin/DeleteUser
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}