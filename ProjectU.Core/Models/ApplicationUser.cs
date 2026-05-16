using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Models
{
    // Розширений користувач системи на базі ASP.NET Core Identity
    public class ApplicationUser : IdentityUser
    {
        // Ім'я користувача
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
                   ErrorMessageResourceName = "Required_FirstName")]
        [StringLength(50, MinimumLength = 2,
                   ErrorMessageResourceType = typeof(Resources.ModelValidation),
                   ErrorMessageResourceName = "StringLength_FirstName")]
        public string FirstName { get; set; } = string.Empty;

        // Прізвище користувача
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
                  ErrorMessageResourceName = "Required_LastName")]
        [StringLength(50, MinimumLength = 2,
                  ErrorMessageResourceType = typeof(Resources.ModelValidation),
                  ErrorMessageResourceName = "StringLength_LastName")]
        public string LastName { get; set; } = string.Empty;

        // Зовнішній ключ до групи (тільки для студентів)
        public int? GroupId { get; set; }

        // Навігаційна властивість до групи
        public Group? Group { get; set; }
        // Мова інтерфейсу користувача
        [StringLength(10)]
        public string PreferredLanguage { get; set; } = "uk-UA";
    }
}