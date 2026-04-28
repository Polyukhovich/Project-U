namespace ProjectU.Core.Interfaces
{
    // Базовий generic інтерфейс для CRUD операцій
    public interface IRepository<T> where T : class
    {
        // Отримати всі записи
        Task<IEnumerable<T>> GetAllAsync();

        // Отримати запис за Id
        Task<T?> GetByIdAsync(int id);

        // Додати новий запис
        Task AddAsync(T entity);

        // Оновити існуючий запис
        void Update(T entity);

        // Видалити запис
        void Delete(T entity);

        // Зберегти зміни в БД
        Task SaveAsync();
    }
}