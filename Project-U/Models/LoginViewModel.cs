using System.ComponentModel.DataAnnotations;

namespace ProjectU.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Запам'ятати мене
        public bool RememberMe { get; set; }
    }
}