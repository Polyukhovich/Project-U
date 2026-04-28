using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Interfaces;
using ProjectU.Core.Models;

namespace ProjectU.Data.Repositories
{
    // Реалізація репозиторію для лабораторних робіт
    public class LabWorkRepository : BaseRepository<LabWork>, ILabWorkRepository
    {
        public LabWorkRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<LabWork>> GetLabWorksByCourseAsync(int courseId)
            => await _context.LabWorks
                .Where(l => l.CourseId == courseId)
                .Include(l => l.Student)
                .ToListAsync();

        public async Task<IEnumerable<LabWork>> GetLabWorksByStudentAsync(string studentId)
            => await _context.LabWorks
                .Where(l => l.StudentId == studentId)
                .Include(l => l.Course)
                .ToListAsync();

        public async Task<IEnumerable<LabWork>> GetLabWorksForPlagiarismCheckAsync(int courseId, string excludeStudentId)
            => await _context.LabWorks
                .Where(l => l.CourseId == courseId && l.StudentId != excludeStudentId)
                .ToListAsync();
    }
}