using System.Linq.Expressions;

namespace AuthenticationService.Core.Interfaces.Repositories;

/// <summary>
/// Generic Repository (Repository Pattern) Arayüzü.
/// 
/// Generic Arayüzler (Generic Interfaces) Nedir?
/// `<T>` ifadesi, bu arayüzün belirli bir tipe bağlı olmadığını, çalışacağı tipi
/// uygulama anında (runtime'da veya implementasyon anında) alacağını belirtir 
/// (Örneğin; `IGenericRepository<User>`, `IGenericRepository<Product>`).
/// Bu sayede her veritabanı tablosu veya entity için ayrı ayrı arayüzler (IUserRepository, IProductRepository vb.)
/// oluşturmak yerine, ortak (Generic) bir yapı kurarak kod tekrarının önüne geçilir ve 
/// DRY (Don't Repeat Yourself) prensibine uyulmuş olur.
/// 
/// Neden Ortak Metotlar (CRUD) Burada Tanımlanır?
/// Veritabanı işlemlerinin (Oluşturma - Add, Okuma - Get/Find, Güncelleme - Update, Silme - Remove) temel
/// mantığı çoğu tablo için aynıdır. Bu CRUD (Create, Read, Update, Delete) metotlarını merkezi bir
/// Generic Interface içinde tanımlamak; projedeki tüm veri erişim sınıflarının standart bir sözleşmeye (contract) 
/// uymasını sağlar. Bu da kodun bakımını kolaylaştırır, test edilebilirliğini artırır ve esneklik sağlar.
/// 
/// `where T : class` kısıtlaması, T yerine gelecek tipin sadece bir referans tipi (class/entity) olabileceğini garanti eder.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    // Verilen benzersiz kimliğe (id) göre tek bir kaydı asenkron olarak getirir. Bulamazsa null döner.
    Task<T?> GetByIdAsync(object id);
    
    // İlgili tablodaki tüm kayıtları asenkron olarak getirir.
    Task<IEnumerable<T>> GetAllAsync();
    
    // Belirli bir şarta (predicate) uyan kayıtların listesini asenkron olarak getirir.
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    
    // Belirli bir şarta (predicate) uyan ilk kaydı asenkron olarak getirir. Bulamazsa null döner.
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    
    // Belirli bir şarta (predicate) uyan en az bir kayıt olup olmadığını asenkron olarak kontrol eder.
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    
    // Belirli bir şarta (predicate) uyan kayıtların toplam sayısını asenkron olarak hesaplar.
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    
    // Veritabanına yeni bir kayıt ekler (Asenkron).
    Task AddAsync(T entity);
    
    // Veritabanına birden fazla yeni kaydı tek seferde ekler (Asenkron).
    Task AddRangeAsync(IEnumerable<T> entities);
    
    // Mevcut bir kaydın durumunu 'Modified' olarak işaretleyip güncellenmesini sağlar.
    void Update(T entity);
    
    // Mevcut bir kaydı veritabanından silmek üzere işaretler.
    void Remove(T entity);
    
    // Birden fazla kaydı veritabanından tek seferde silmek üzere işaretler.
    void RemoveRange(IEnumerable<T> entities);
    
    // İhtiyaç duyulan daha karmaşık sorguları LINQ kullanarak veri kaynağı üzerinde çalıştırabilmek için IQueryable nesnesi döner.
    IQueryable<T> Query();
}
