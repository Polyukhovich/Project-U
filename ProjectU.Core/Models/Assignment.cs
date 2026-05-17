using ProjectU.Core.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectU.Core.Models
{
    // Завдання від викладача для групи
    public class Assignment
    {
        public int Id { get; set; }

        // Назва завдання (наприклад: Лабораторна робота №1)
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "Required_AssignmentTitle")]
        [StringLength(200, MinimumLength = 3,
           ErrorMessageResourceType = typeof(Resources.ModelValidation),
           ErrorMessageResourceName = "StringLength_AssignmentTitle")]
        public string Title { get; set; } = string.Empty;

        // Опис завдання
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
          ErrorMessageResourceName = "Required_AssignmentDescription")]
        public string Description { get; set; } = string.Empty;

        // Дедлайн здачі
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
                  ErrorMessageResourceName = "Required_AssignmentDeadline")]
        [FutureDate(ErrorMessageResourceType = typeof(Resources.ModelValidation),
                  ErrorMessageResourceName = "FutureDate_AssignmentDeadline")]
        public DateTime Deadline { get; set; }

        // Дата створення
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Тип матеріалу: None, File, Link
        public string MaterialType { get; set; } = "None";

        // Посилання на матеріал
        public string? MaterialUrl { get; set; }

        // Файл матеріалу
        public string? MaterialFilePath { get; set; }
        public string? MaterialFileName { get; set; }

        // Дозволити завантаження
        public bool AllowDownload { get; set; } = true;

        // Зовнішній ключ до курсу
        [Required(ErrorMessageResourceType = typeof(Resources.ModelValidation),
                  ErrorMessageResourceName = "Required_AssignmentCourse")]
        public int CourseId { get; set; }

        // Навігаційна властивість до курсу
        public Course? Course { get; set; }
        // Список підзавдань
        public ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();

        // Список здач від студентів
        public ICollection<LabWork> Submissions { get; set; } = new List<LabWork>();
    }
}