using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Навчальний курс/предмет
    public class Course
    {
        public int Id { get; set; }

        // Назва курсу
        [Required(ErrorMessage = "Назва курсу обов'язкова")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Назва має бути від 3 до 200 символів")]
        public string Name { get; set; } = string.Empty;

        // Зовнішній ключ до викладача
        [Required(ErrorMessage = "Оберіть викладача")]
        public string TeacherId { get; set; } = string.Empty;

        // Навігаційна властивість до викладача
        public ApplicationUser? Teacher { get; set; }

        // Зовнішній ключ до групи
        [Required(ErrorMessage = "Оберіть групу")]
        public int GroupId { get; set; }

        // Навігаційна властивість до групи
        public Group? Group { get; set; }

        // Список занять у розкладі
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

        // Список оцінок по курсу
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();

        // Список лабораторних робіт по курсу
        public ICollection<LabWork> LabWorks { get; set; } = new List<LabWork>();
    }
}