namespace AuthenticationService.Core.Interfaces.Repositories;

/// <summary>
/// Unit of Work (İş Birimi) tasarım desenini temsil eden arayüz.
/// Bu desenin temel amacı, birbiriyle ilişkili birden fazla veritabanı işlemini (insert, update, delete)
/// tek bir transaction (işlem) bloğu içerisinde toplayıp, tüm işlemlerin başarılı olması durumunda
/// veritabanına tek seferde yansıtılmasını (commit) sağlamaktır. Hata durumunda ise geri alınmasını (rollback) sağlar.
/// 
/// IDisposable arayüzünden miras alınmasının sebebi:
/// Veritabanı bağlantısı veya transaction gibi yönetilmeyen (unmanaged) kaynakların, 
/// işlemler bittikten sonra (veya hata olduğunda) güvenli ve manuel bir şekilde serbest bırakılmasını (dispose) sağlamaktır.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Verilen TEntity türü için ilgili Repository örneğini (instance) döndürür.
    /// Böylece her entity için ayrı ayrı repository'leri manuel olarak çağırmak yerine, 
    /// Unit of Work üzerinden merkezi bir erişim sağlanır.
    /// </summary>
    /// <typeparam name="TEntity">Veritabanı tablosuna karşılık gelen sınıf.</typeparam>
    /// <returns>TEntity için IGenericRepository örneği.</returns>
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;

    /// <summary>
    /// Yapılan tüm veri değişikliklerini asenkron olarak veritabanına kaydeder.
    /// Entity Framework Core kullanılıyorsa, arkaplanda 'SaveChangesAsync' metodunu çağırır.
    /// Unit of Work deseni sayesinde, farklı repository'lerde yapılan tüm değişiklikler hafızada tutulur 
    /// ve bu metot çağrıldığında tek bir seferde (transaction ile) veritabanına aktarılır.
    /// </summary>
    /// <returns>Veritabanında etkilenen (değişen/eklenen/silinen) kayıt sayısını döner.</returns>
    Task<int> SaveChangesAsync();
}
