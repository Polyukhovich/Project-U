using System.ComponentModel.DataAnnotations;
namespace ProjectU.Core.Models
{
    public class SubTask
    {
        public int Id { get; set; }

        // Назва підзавдання
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        // Опис підзавдання
        public string? Description { get; set; }

        // Максимальний бал за підзавдання
        [Required]
        [Range(1, 100)]
        public int MaxScore { get; set; }

        // Зовнішній ключ до завдання
        [Required]
        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }
    }
}