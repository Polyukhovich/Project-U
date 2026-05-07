using System.ComponentModel.DataAnnotations;

namespace ProjectU.Core.Validation
{
    // Кастомний атрибут валідації — дата має бути в майбутньому
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
                return date > DateTime.Now;
            return false;
        }
    }
}