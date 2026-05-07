using System.Collections.Concurrent;

namespace Project_U.Middlewares
{
    // Middleware для обмеження кількості запитів 
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _authLimit;
        private readonly int _anonLimit;
        private readonly TimeSpan _window;

        // Зберігаємо лічильники запитів для кожного користувача/IP
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requests = new();

        public RateLimitingMiddleware(RequestDelegate next, int authLimit, int anonLimit, TimeSpan window)
        {
            _next = next;
            _authLimit = authLimit;
            _anonLimit = anonLimit;
            _window = window;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Визначаємо ключ — для авторизованих користувачів беремо ім'я, для анонімних — IP
            var key = context.User.Identity?.IsAuthenticated == true
                ? context.User.Identity.Name!
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var limit = context.User.Identity?.IsAuthenticated == true ? _authLimit : _anonLimit;

            var now = DateTime.UtcNow;

            _requests.AddOrUpdate(key,
                _ => (1, now),
                (_, existing) =>
                {
                    // Якщо вийшли за межі вікна — скидаємо лічильник
                    if (now - existing.WindowStart > _window)
                        return (1, now);

                    return (existing.Count + 1, existing.WindowStart);
                });

            var (count, windowStart) = _requests[key];

            // Якщо перевищено ліміт — повертаємо 429
            if (count > limit)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "text/html; charset=utf-8";

                var html = await File.ReadAllTextAsync(
                    Path.Combine(Directory.GetCurrentDirectory(),
                    "Views", "Shared", "TooManyRequests.cshtml"));

                await context.Response.WriteAsync(html);
                return;
            }

            await _next(context);
        }
    }

    // Extension method для зручної реєстрації
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLimiter(
            this IApplicationBuilder app,
            int authLimit = 200,
            int anonLimit = 50,
            TimeSpan? window = null)
        {
            return app.UseMiddleware<RateLimitingMiddleware>(
                authLimit,
                anonLimit,
                window ?? TimeSpan.FromMinutes(1));
        }
    }
}