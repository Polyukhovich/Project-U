using System.ComponentModel.DataAnnotations;
namespace ProjectU.Core.Models
{
    public class SubTaskGrade
    {
        public int Id { get; set; }

        // Зовнішній ключ до підзавдання
        [Required]
        public int SubTaskId { get; set; }
        public SubTask? SubTask { get; set; }

        // Зовнішній ключ до здачі роботи
        [Required]
        public int LabWorkId { get; set; }
        public LabWork? LabWork { get; set; }

        // Бал за підзавдання
        [Required]
        [Range(0, 100)]
        public int Value { get; set; }
    }
}