using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Models;

namespace ProjectU.Data
{
    // Головний контекст бази даних
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Таблиці бази даних
        public DbSet<Group> Groups { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<LabWork> LabWorks { get; set; }
        public DbSet<PlagiarismResult> PlagiarismResults { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Assignment> Assignments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Налаштування зв'язку PlagiarismResult з двома LabWork
            builder.Entity<PlagiarismResult>()
                .HasOne(p => p.LabWork)
                .WithMany(l => l.PlagiarismResults)
                .HasForeignKey(p => p.LabWorkId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PlagiarismResult>()
                .HasOne(p => p.ComparedWith)
                .WithMany()
                .HasForeignKey(p => p.ComparedWithId)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування зв'язку Course з викладачем
            builder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Виправлення cascade paths для Schedule
            builder.Entity<Schedule>()
                .HasOne(s => s.Course)
                .WithMany(c => c.Schedules)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedule>()
                .HasOne(s => s.Group)
                .WithMany()
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування зв'язку Assignment з LabWork
            builder.Entity<LabWork>()
                .HasOne(l => l.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(l => l.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}