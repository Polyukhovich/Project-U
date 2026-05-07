using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Розклад занять — доступний в offline-режимі (PWA)
    public class Schedule
    {
        public int Id { get; set; }

        // День тижня (наприклад "Понеділок")
        [Required(ErrorMessage = "День тижня обов'язковий")]
        public string DayOfWeek { get; set; } = string.Empty;

        // Час початку заняття
        [Required(ErrorMessage = "Час початку обов'язковий")]
        public TimeOnly StartTime { get; set; }

        // Час завершення заняття
        [Required(ErrorMessage = "Час завершення обов'язковий")]
        public TimeOnly EndTime { get; set; }

        // Аудиторія
        public string Room { get; set; } = string.Empty;

        // Зовнішній ключ до курсу
        [Required(ErrorMessage = "Оберіть курс")]
        public int CourseId { get; set; }

        // Навігаційна властивість до курсу
        public Course? Course { get; set; }

        // Зовнішній ключ до групи
        [Required(ErrorMessage = "Оберіть групу")]
        public int GroupId { get; set; }

        // Навігаційна властивість до групи
        public Group? Group { get; set; }
    }
}