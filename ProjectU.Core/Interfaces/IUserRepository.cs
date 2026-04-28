using ProjectU.Core.Models;

namespace ProjectU.Core.Interfaces
{
    // Інтерфейс для роботи з користувачами
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        // Отримати всіх студентів групи
        Task<IEnumerable<ApplicationUser>> GetStudentsByGroupAsync(int groupId);

        // Отримати всіх викладачів
        Task<IEnumerable<ApplicationUser>> GetAllTeachersAsync();
    }
}