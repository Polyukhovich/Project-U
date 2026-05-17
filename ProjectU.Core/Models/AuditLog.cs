using System.ComponentModel.DataAnnotations;
namespace ProjectU.Core.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        // Хто виконав дію
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Яка дія була виконана
        [Required]
        public string Action { get; set; } = string.Empty;

        // Над чим (наприклад Course, Grade, LabWork)
        public string? EntityType { get; set; }

        // Id об'єкту
        public string? EntityId { get; set; }

        // Деталі дії
        public string? Details { get; set; }

        // Коли
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // IP адреса
        public string? IpAddress { get; set; }
    }
}