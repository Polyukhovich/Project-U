using ProjectU.Core.Models;

namespace ProjectU.Core.Interfaces
{
    // Інтерфейс для роботи з оцінками
    public interface IGradeRepository : IRepository<Grade>
    {
        // Отримати всі оцінки студента
        Task<IEnumerable<Grade>> GetGradesByStudentAsync(string studentId);

        // Отримати всі оцінки по курсу
        Task<IEnumerable<Grade>> GetGradesByCourseAsync(int courseId);
    }
}