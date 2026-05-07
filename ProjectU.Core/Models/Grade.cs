using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Оцінка студента — зміна тригерить сповіщення через SignalR
    public class Grade
    {
        public int Id { get; set; }

        // Значення оцінки (0-100)
        [Required(ErrorMessage = "Оцінка обов'язкова")]
        [Range(0, 100, ErrorMessage = "Оцінка має бути від 0 до 100")]
        public int Value { get; set; }

        // Дата виставлення оцінки
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Зовнішній ключ до студента
        [Required(ErrorMessage = "Оберіть студента")]
        public string StudentId { get; set; } = string.Empty;

        // Навігаційна властивість до студента
        public ApplicationUser? Student { get; set; }

        // Зовнішній ключ до курсу
        [Required(ErrorMessage = "Оберіть курс")]
        public int CourseId { get; set; }

        // Навігаційна властивість до курсу
        public Course? Course { get; set; }

        // Зовнішній ключ до лабораторної (необов'язковий)
        public int? LabWorkId { get; set; }

        // Навігаційна властивість до лабораторної
        public LabWork? LabWork { get; set; }
    }
}