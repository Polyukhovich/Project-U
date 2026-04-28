namespace ProjectU.Core.Models
{
    // Результат порівняння двох лабораторних робіт на схожість
    public class PlagiarismResult
    {
        public int Id { get; set; }

        // Зовнішній ключ до роботи що перевіряється
        public int LabWorkId { get; set; }
        public LabWork? LabWork { get; set; }

        // Зовнішній ключ до роботи з якою порівнюється
        public int ComparedWithId { get; set; }
        public LabWork? ComparedWith { get; set; }

        // Відсоток схожості (0.0 - 100.0)
        public double SimilarityPercent { get; set; }

        // Дата та час перевірки
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}