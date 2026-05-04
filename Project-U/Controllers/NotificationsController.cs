using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectU.Data;

namespace Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: отримати сповіщення поточного користувача (JSON)
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = _context.Users
                .FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;

            if (userId == null) return Json(new List<object>());

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            return Json(notifications);
        }
        // POST: позначити одне сповіщення як прочитане
        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
        // POST: позначити всі як прочитані
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _context.Users
                .FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;

            if (userId == null) return Json(new { success = false });

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
                n.IsRead = true;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // GET: кількість непрочитаних
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _context.Users
                .FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;

            if (userId == null) return Json(new { count = 0 });

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { count });
        }
    }
}