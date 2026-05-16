using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Оцінка студента — зміна тригерить сповіщення через SignalR
    public class Grade
    {
        public int Id { get; set; }

        // Значення оцінки (0-100)
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "Required_GradeValue")]
        [Range(0, 100,
           ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "Range_GradeValue")]
        public int Value { get; set; }

        // Дата виставлення оцінки
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Зовнішній ключ до студента
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "Required_GradeStudent")]
        public string StudentId { get; set; } = string.Empty;

        // Навігаційна властивість до студента
        public ApplicationUser? Student { get; set; }

        // Зовнішній ключ до курсу
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "Required_GradeCourse")]
        public int CourseId { get; set; }

        // Навігаційна властивість до курсу
        public Course? Course { get; set; }

        // Зовнішній ключ до лабораторної (необов'язковий)
        public int? LabWorkId { get; set; }

        // Навігаційна властивість до лабораторної
        public LabWork? LabWork { get; set; }
    }
}