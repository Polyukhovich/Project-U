using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Interfaces;
using ProjectU.Core.Models;

namespace ProjectU.Data.Repositories
{
    // Реалізація репозиторію для користувачів
    public class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<ApplicationUser>> GetStudentsByGroupAsync(int groupId)
            => await _context.Users
                .Where(u => u.GroupId == groupId)
                .ToListAsync();

        public async Task<IEnumerable<ApplicationUser>> GetAllTeachersAsync()
            => await _context.Users
                .Where(u => u.GroupId == null)
                .ToListAsync();
    }
}