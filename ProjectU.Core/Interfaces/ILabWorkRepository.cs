using ProjectU.Core.Models;

namespace ProjectU.Core.Interfaces
{
    // Інтерфейс для роботи з лабораторними роботами
    public interface ILabWorkRepository : IRepository<LabWork>
    {
        // Отримати всі роботи по курсу
        Task<IEnumerable<LabWork>> GetLabWorksByCourseAsync(int courseId);

        // Отримати всі роботи студента
        Task<IEnumerable<LabWork>> GetLabWorksByStudentAsync(string studentId);

        // Отримати роботи одногрупників для перевірки антиплагіату
        Task<IEnumerable<LabWork>> GetLabWorksForPlagiarismCheckAsync(int courseId, string excludeStudentId);
    }
}