using System.ComponentModel.DataAnnotations;
namespace ProjectU.Core.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        // Зовнішній ключ до студента
        [Required]
        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser? Student { get; set; }

        // Зовнішній ключ до розкладу
        [Required]
        public int ScheduleId { get; set; }
        public Schedule? Schedule { get; set; }

        // Дата заняття
        [Required]
        public DateOnly Date { get; set; }

        // Присутній чи відсутній
        public bool IsPresent { get; set; } = true;

        // Коментар викладача (необов'язково)
        public string? Comment { get; set; }

        // Дата запису
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}