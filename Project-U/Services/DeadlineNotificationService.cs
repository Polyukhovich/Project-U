using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Project_U.Hubs;
using ProjectU.Core.Models;
using ProjectU.Data;

namespace Project_U.Services
{
    // Фоновий сервіс для сповіщень про наближення дедлайну
    public class DeadlineNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<DeadlineNotificationService> _logger;

        public DeadlineNotificationService(
            IServiceScopeFactory scopeFactory,
            IHubContext<NotificationHub> hubContext,
            ILogger<DeadlineNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckDeadlines();
                // Перевіряємо кожні 15 хв
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task CheckDeadlines()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Знаходимо завдання з дедлайном через 24 години
            var tomorrow = DateTime.UtcNow.AddHours(24);
            var soon = DateTime.UtcNow;

            var assignments = await context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Group)
                        .ThenInclude(g => g.Students)
                .Where(a => a.Deadline > soon && a.Deadline <= tomorrow)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                var students = assignment.Course?.Group?.Students;
                if (students == null) continue;

                var hoursLeft = (int)(assignment.Deadline - DateTime.UtcNow).TotalHours;
                var message = $"⏰ До дедлайну '{assignment.Title}' залишилось {hoursLeft} год!";

                foreach (var student in students)
                {
                    // Перевіряємо чи вже здав роботу
                    var alreadySubmitted = await context.LabWorks
                        .AnyAsync(l => l.AssignmentId == assignment.Id && l.StudentId == student.Id);

                    // Перевіряємо чи вже надсилали сповіщення про цей дедлайн
                    var alreadyNotified = await context.Notifications
                        .AnyAsync(n => n.UserId == student.Id
                                    && n.Message.Contains(assignment.Title)
                                    && n.Message.Contains("дедлайну"));
                    if (!alreadySubmitted)
                    {
                        await _hubContext.Clients
                            .Group($"user_{student.Id}")
                            .SendAsync("ReceiveNotification", message);

                        context.Notifications.Add(new Notification
                        {
                            UserId = student.Id,
                            Message = message,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"Deadline notifications sent for assignment: {assignment.Title}");
            }
        }
    }
}