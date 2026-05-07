using ProjectU.Core.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectU.Core.Models
{
    // Завдання від викладача для групи
    public class Assignment
    {
        public int Id { get; set; }

        // Назва завдання (наприклад: Лабораторна робота №1)
        [Required(ErrorMessage = "Назва завдання обов'язкова")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Назва має бути від 3 до 200 символів")]
        public string Title { get; set; } = string.Empty;

        // Опис завдання
        [Required(ErrorMessage = "Опис завдання обов'язковий")]
        public string Description { get; set; } = string.Empty;

        // Дедлайн здачі
        [Required(ErrorMessage = "Дедлайн обов'язковий")]
        [FutureDate(ErrorMessage = "Дедлайн має бути в майбутньому")]
        public DateTime Deadline { get; set; }

        // Дата створення
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Зовнішній ключ до курсу
        [Required(ErrorMessage = "Оберіть курс")]
        public int CourseId { get; set; }

        // Навігаційна властивість до курсу
        public Course? Course { get; set; }

        // Список здач від студентів
        public ICollection<LabWork> Submissions { get; set; } = new List<LabWork>();
    }
}