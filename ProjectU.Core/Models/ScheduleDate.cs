namespace ProjectU.Core.Models
{
    // Конкретна дата заняття
    public class ScheduleDate
    {
        public int Id { get; set; }

        // Дата заняття
        public DateOnly Date { get; set; }

        // Зовнішній ключ до розкладу
        public int ScheduleId { get; set; }
        public Schedule? Schedule { get; set; }
    }
}