using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Розклад занять — доступний в offline-режимі (PWA)
    public class Schedule
    {
        public int Id { get; set; }

        // Час початку заняття
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
            ErrorMessageResourceName = "Required_ScheduleStart")]
        public TimeOnly StartTime { get; set; }

        // Час завершення заняття
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "Required_ScheduleEnd")]
        public TimeOnly EndTime { get; set; }

        // Аудиторія або посилання (необов'язково)
        public string Room { get; set; } = string.Empty;
        // Тип заняття: Lecture, Lab, Practice
        public string LessonType { get; set; } = "Lecture";

        // Зовнішній ключ до курсу
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "Required_ScheduleCourse")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // Зовнішній ключ до групи
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
          ErrorMessageResourceName = "Required_ScheduleGroup")]
        public int GroupId { get; set; }
        public Group? Group { get; set; }

        // Конкретні дати занять
        public ICollection<ScheduleDate> Dates { get; set; } = new List<ScheduleDate>();
    }
}