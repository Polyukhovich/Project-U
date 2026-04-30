using System.ComponentModel.DataAnnotations;

namespace Project_U.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Введіть ім'я")]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть прізвище")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль мінімум 6 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Оберіть роль")]
        [Display(Name = "Роль")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Група (для студентів)")]
        public int? GroupId { get; set; }
    }
}