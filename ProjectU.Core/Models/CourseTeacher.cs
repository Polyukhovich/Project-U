namespace ProjectU.Core.Models
{
    // Зв'язок між курсом та викладачем (many-to-many)
    public class CourseTeacher
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public string TeacherId { get; set; } = string.Empty;
        public ApplicationUser? Teacher { get; set; }

        // Роль викладача на курсі
        public string Role { get; set; } = "Teacher"; // Teacher / Assistant
    }
}