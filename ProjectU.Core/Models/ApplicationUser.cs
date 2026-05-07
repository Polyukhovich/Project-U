using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Розширений користувач системи на базі ASP.NET Core Identity
    public class ApplicationUser : IdentityUser
    {
        // Ім'я користувача
        [Required(ErrorMessage = "Ім'я обов'язкове")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Ім'я від 2 до 50 символів")]
        public string FirstName { get; set; } = string.Empty;

        // Прізвище користувача
        [Required(ErrorMessage = "Прізвище обов'язкове")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Прізвище від 2 до 50 символів")]
        public string LastName { get; set; } = string.Empty;

        // Зовнішній ключ до групи (тільки для студентів)
        public int? GroupId { get; set; }

        // Навігаційна властивість до групи
        public Group? Group { get; set; }
    }
}