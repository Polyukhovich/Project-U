namespace ProjectU.Core.Models
{
    // Розклад занять — доступний в offline-режимі (PWA)
    public class Schedule
    {
        public int Id { get; set; }

        // День тижня (наприклад "Понеділок")
        public string DayOfWeek { get; set; } = string.Empty;

        // Час початку заняття
        public TimeOnly StartTime { get; set; }

        // Час завершення заняття
        public TimeOnly EndTime { get; set; }

        // Аудиторія
        public string Room { get; set; } = string.Empty;

        // Зовнішній ключ до курсу
        public int CourseId { get; set; }

        // Навігаційна властивість до курсу
        public Course? Course { get; set; }

        // Зовнішній ключ до групи
        public int GroupId { get; set; }

        // Навігаційна властивість до групи
        public Group? Group { get; set; }
    }
}