using Microsoft.EntityFrameworkCore;
using BlogSite.CORE.Data;
using BlogSite.CORE.Repositories.Abstract;

namespace BlogSite.CORE.Repositories.Concrete
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly BlogDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(BlogDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
        public async Task<List<T>> GetAllAsync() => await _dbSet.ToListAsync();
        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
        public void Remove(T entity) => _dbSet.Remove(entity);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
