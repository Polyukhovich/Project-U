using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Навчальна група (наприклад МІТ-41)
    public class Group
    {
        public int Id { get; set; }

        // Назва групи
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
          ErrorMessageResourceName = "Required_GroupName")]
        [StringLength(50, MinimumLength = 2,
          ErrorMessageResourceType = typeof(Resources.ModelValidation),
          ErrorMessageResourceName = "StringLength_GroupName")]
        public string Name { get; set; } = string.Empty;

        // Список студентів що належать до групи
        public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();

        // Список курсів що викладаються для групи
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}