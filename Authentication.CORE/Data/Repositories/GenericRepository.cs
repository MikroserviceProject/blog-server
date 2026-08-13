using System.Linq.Expressions;
using AuthenticationService.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Core.Data.Repositories;

/// <summary>
/// Generic Repository (Genel Depo) Kalıbı (Pattern).
/// <T> (Generic Type) kullanmamızın temel sebebi "Code Reusability" yani kodun yeniden kullanılabilirliğidir.
/// Veritabanımızdaki her tablo (User, Product, Order vb.) için ayrı ayrı Ekle/Sil/Güncelle metotları 
/// yazmak yerine, hepsinde ortak olan bu işlemleri tek bir sınıfta (<T> ile) tanımlayarak 
/// tekrarı (DRY - Don't Repeat Yourself) önlüyoruz.
/// "where T : class" kısıtlaması, T'nin sadece bir referans tipi (yani veritabanı entity'si) olabileceğini belirtir.
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    // Veritabanı ile iletişim kurmamızı sağlayan ana bağlam (Context) sınıfı.
    protected readonly AppDbContext _context;
    
    // DbSet<T>, veritabanındaki tablolara (T tipindeki entity'ye) karşılık gelir. 
    // LINQ sorgularını tabloya yansıtmak ve CRUD (Oluşturma, Okuma, Güncelleme, Silme) işlemlerini yapmak için kullanılır.
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>(); // Gelen context üzerinden T tipine ait tabloyu (_dbSet) yakalıyoruz.
    }

    /// <summary>
    /// Veritabanında birincil anahtara (Primary Key) göre arama yapar.
    /// FindAsync, bellekte (memory) o veri varsa veritabanına gitmeden getirir, yoksa sorgu atar. Oldukça performanslıdır.
    /// </summary>
    public async Task<T?> GetByIdAsync(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Tablodaki tüm verileri getirir.
    /// </summary>
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// Belirli bir şarta (predicate) uyan kayıtları bulur. 
    /// LINQ metodu olan .Where() kullanımı: Filtreleme yapar (örneğin SQL'deki WHERE karşılığı).
    /// </summary>
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Şarta uyan ilk kaydı getirir, bulamazsa null (varsayılan değer) döndürür.
    /// </summary>
    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Verilen koşula uygun en az 1 kayıt var mı yok mu (bool) kontrol eder. 
    /// Performans açısından çok faydalıdır (verileri çekmeden sadece varlık kontrolü yapar).
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }
    
    /// <summary>
    /// Verilen koşula uyan kayıtların sayısını döndürür (SQL'deki COUNT).
    /// </summary>
    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }

    /// <summary>
    /// Yeni bir varlığı tabloya (Entity State = Added) eklemek üzere işaretler. 
    /// Veritabanına fiziksel kayıt SaveChanges() çağrıldığında yapılır.
    /// </summary>
    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    /// <summary>
    /// Birden fazla varlığı aynı anda tabloya eklemek için kullanılır.
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    /// <summary>
    /// Var olan bir kaydı günceller (Entity State = Modified). 
    /// .Update() metodu asenkron (async) bir yapıya ihtiyaç duymaz, çünkü sadece RAM üzerindeki nesne state'ini değiştirir.
    /// </summary>
    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    /// <summary>
    /// Var olan bir kaydı silinmek üzere işaretler (Entity State = Deleted).
    /// </summary>
    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Birden fazla kaydı toplu şekilde silinmek üzere işaretler.
    /// </summary>
    public void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Sorgulanabilir (IQueryable) olarak tabloyu döndürür.
    /// Veritabanına gitmeden önce (Örn: Include, OrderBy vb.) ekstra filtreler/sorgular ekleyebilmemiz için açık kapı bırakır.
    /// </summary>
    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
}
