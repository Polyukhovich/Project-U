using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Project_U.Helpers;
using ProjectU.Core.Models;

namespace Project_U.Filters
{
    public class LoginLogoutFilter : IAsyncPageFilter
    {
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginLogoutFilter(AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path.Value ?? "";
            var method = context.HttpContext.Request.Method;

            // Зберігаємо дані ДО виходу
            string? logoutUserId = null;
            string? logoutUserEmail = null;
            if (path.Contains("/Account/Logout") && method == "POST")
            {
                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                logoutUserId = user?.Id;
                logoutUserEmail = user?.Email;
            }

            // Зберігаємо email ДО входу
            string? loginEmail = null;
            if (path.Contains("/Account/Login") && method == "POST")
            {
                loginEmail = context.HttpContext.Request.Form["Input.Email"].ToString();
            }

            await next();

            // Логуємо вхід після успішного входу
            if (path.Contains("/Account/Login") && method == "POST" && loginEmail != null)
            {
                var user = await _userManager.FindByEmailAsync(loginEmail);
                if (user != null)
                {
                    await _auditService.LogAsync(
                        user.Id, "Login", "Auth", null,
                        $"Вхід: {user.Email}");
                }
            }

            // Логуємо вихід
            if (path.Contains("/Account/Logout") && method == "POST" && logoutUserId != null)
            {
                await _auditService.LogAsync(
                    logoutUserId, "Logout", "Auth", null,
                    $"Вихід: {logoutUserEmail}");
            }
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }
    }
}