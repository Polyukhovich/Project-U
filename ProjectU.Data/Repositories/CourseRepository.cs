using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Interfaces;
using ProjectU.Core.Models;

namespace ProjectU.Data.Repositories
{
    // Реалізація репозиторію для курсів
    public class CourseRepository : BaseRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Course>> GetCoursesByGroupAsync(int groupId)
            => await _context.Courses
                .Where(c => c.GroupId == groupId)
                .Include(c => c.Teacher)
                .ToListAsync();

        public async Task<IEnumerable<Course>> GetCoursesByTeacherAsync(string teacherId)
            => await _context.Courses
                .Where(c => c.TeacherId == teacherId)
                .Include(c => c.Group)
                .ToListAsync();
    }
}