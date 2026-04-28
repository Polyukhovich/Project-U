using ProjectU.Core.Models;

namespace ProjectU.Core.Interfaces
{
    // Інтерфейс для роботи з курсами
    public interface ICourseRepository : IRepository<Course>
    {
        // Отримати всі курси групи
        Task<IEnumerable<Course>> GetCoursesByGroupAsync(int groupId);

        // Отримати всі курси викладача
        Task<IEnumerable<Course>> GetCoursesByTeacherAsync(string teacherId);
    }
}