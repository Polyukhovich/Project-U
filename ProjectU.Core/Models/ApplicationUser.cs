using Microsoft.AspNetCore.Identity;

namespace ProjectU.Core.Models
{
    // Розширений користувач системи на базі ASP.NET Core Identity
    public class ApplicationUser : IdentityUser
    {
        // Ім'я користувача
        public string FirstName { get; set; } = string.Empty;

        // Прізвище користувача
        public string LastName { get; set; } = string.Empty;

        // Зовнішній ключ до групи (тільки для студентів)
        public int? GroupId { get; set; }

        // Навігаційна властивість до групи
        public Group? Group { get; set; }
    }
}