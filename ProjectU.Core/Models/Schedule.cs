using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Розклад занять — доступний в offline-режимі (PWA)
    public class Schedule
    {
        public int Id { get; set; }

        // Час початку заняття
        [Required(ErrorMessage = "Час початку обов'язковий")]
        public TimeOnly StartTime { get; set; }

        // Час завершення заняття
        [Required(ErrorMessage = "Час завершення обов'язковий")]
        public TimeOnly EndTime { get; set; }

        // Аудиторія або посилання (необов'язково)
        public string Room { get; set; } = string.Empty;

        // Зовнішній ключ до курсу
        [Required(ErrorMessage = "Оберіть курс")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // Зовнішній ключ до групи
        [Required(ErrorMessage = "Оберіть групу")]
        public int GroupId { get; set; }
        public Group? Group { get; set; }

        // Конкретні дати занять
        public ICollection<ScheduleDate> Dates { get; set; } = new List<ScheduleDate>();
    }
}