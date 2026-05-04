using System;
using System.Collections.Generic;
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
        public string Title { get; set; } = string.Empty;

        // Опис завдання
        public string Description { get; set; } = string.Empty;

        // Дедлайн здачі
        public DateTime Deadline { get; set; }

        // Дата створення
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Зовнішній ключ до курсу
        public int CourseId { get; set; }

        // Навігаційна властивість до курсу
        public Course? Course { get; set; }

        // Список здач від студентів
        public ICollection<LabWork> Submissions { get; set; } = new List<LabWork>();
    }
}