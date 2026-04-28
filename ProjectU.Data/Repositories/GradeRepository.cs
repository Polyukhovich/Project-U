using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Interfaces;
using ProjectU.Core.Models;

namespace ProjectU.Data.Repositories
{
    // Реалізація репозиторію для оцінок
    public class GradeRepository : BaseRepository<Grade>, IGradeRepository
    {
        public GradeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Grade>> GetGradesByStudentAsync(string studentId)
            => await _context.Grades
                .Where(g => g.StudentId == studentId)
                .Include(g => g.Course)
                .ToListAsync();

        public async Task<IEnumerable<Grade>> GetGradesByCourseAsync(int courseId)
            => await _context.Grades
                .Where(g => g.CourseId == courseId)
                .Include(g => g.Student)
                .ToListAsync();
    }
}