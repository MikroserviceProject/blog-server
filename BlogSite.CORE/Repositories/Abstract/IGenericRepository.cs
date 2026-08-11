namespace BlogSite.CORE.Repositories.Abstract
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Remove(T entity);
        Task SaveChangesAsync();
    }
}
