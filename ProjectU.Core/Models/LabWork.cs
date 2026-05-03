namespace ProjectU.Core.Models
{
    // Лабораторна робота студента — при завантаженні запускається антиплагіат
    public class LabWork
    {
        public int Id { get; set; }
        // Назва лабораторної роботи
        public string Title { get; set; } = string.Empty;
        // Текст роботи (використовується для перевірки антиплагіату)
        public string Content { get; set; } = string.Empty;
        // Шлях до завантаженого файлу
        public string FilePath { get; set; } = string.Empty;
        // Оригінальна назва файлу
        public string FileName { get; set; } = string.Empty;
        // Дата завантаження роботи
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        // Зовнішній ключ до студента
        public string StudentId { get; set; } = string.Empty;
        // Навігаційна властивість до студента
        public ApplicationUser? Student { get; set; }
        // Зовнішній ключ до курсу
        public int CourseId { get; set; }
        // Навігаційна властивість до курсу
        public Course? Course { get; set; }
        // Результати перевірки на плагіат для цієї роботи
        public ICollection<PlagiarismResult> PlagiarismResults { get; set; } = new List<PlagiarismResult>();
    }
}