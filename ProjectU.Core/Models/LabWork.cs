using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Здача лабораторної роботи студентом
    public class LabWork
    {
        public int Id { get; set; }
        // Назва роботи
        [Required(ErrorMessage = "Назва роботи обов'язкова")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Назва має бути від 3 до 200 символів")]
        public string Title { get; set; } = string.Empty;
        // Текст роботи (витягується з файлу для антиплагіату)
        public string Content { get; set; } = string.Empty;
        // Шлях до завантаженого файлу
        public string FilePath { get; set; } = string.Empty;
        // Оригінальна назва файлу
        public string FileName { get; set; } = string.Empty;
        // Дата завантаження
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        // Чи оцінена робота
        public bool IsGraded { get; set; } = false;
        // Зовнішній ключ до студента
        [Required(ErrorMessage = "Оберіть студента")]
        public string StudentId { get; set; } = string.Empty;
        // Навігаційна властивість до студента
        public ApplicationUser? Student { get; set; }
        // Зовнішній ключ до завдання
        [Required(ErrorMessage = "Оберіть завдання")]
        public int AssignmentId { get; set; }
        // Навігаційна властивість до завдання
        public Assignment? Assignment { get; set; }
        // Зовнішній ключ до курсу (для зручності)
        [Required(ErrorMessage = "Оберіть курс")]
        public int CourseId { get; set; }
        // Навігаційна властивість до курсу
        public Course? Course { get; set; }
        // Результати перевірки на плагіат
        public ICollection<PlagiarismResult> PlagiarismResults { get; set; } = new List<PlagiarismResult>();
    }
}