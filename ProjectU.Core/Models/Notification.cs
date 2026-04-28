namespace ProjectU.Core.Models
{
    // Сповіщення для користувача — відображається в реальному часі через SignalR
    public class Notification
    {
        public int Id { get; set; }

        // Зовнішній ключ до користувача
        public string UserId { get; set; } = string.Empty;

        // Навігаційна властивість до користувача
        public ApplicationUser? User { get; set; }

        // Текст сповіщення
        public string Message { get; set; } = string.Empty;

        // Чи прочитане сповіщення
        public bool IsRead { get; set; } = false;

        // Дата та час створення сповіщення
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}