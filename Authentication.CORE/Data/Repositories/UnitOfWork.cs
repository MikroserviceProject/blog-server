using System.Collections;
using AuthenticationService.Core.Interfaces.Repositories;

namespace AuthenticationService.Core.Data.Repositories;

/// <summary>
/// Unit of Work (İş Birimi) tasarım deseni, veritabanı işlemlerini tek bir merkezden yönetmeyi sağlar.
/// Repository pattern ile birlikte kullanıldığında, farklı repository'ler üzerinden yapılan 
/// tüm ekleme, silme veya güncelleme işlemlerinin tek bir transaction (işlem) olarak ele alınmasını sağlar.
/// Bu sayede, işlemlerin bir kısmı başarılı olup diğer kısmı hata verirse, tüm işlemler geri alınabilir (rollback).
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private Hashtable? _repositories;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generic olarak repository oluşturur veya daha önce oluşturulmuşsa onu döndürür.
    /// Bu, her entity için ayrı bir repository instance'ı oluşturulmasını ve aynı DbContext'i paylaşmalarını sağlar.
    /// </summary>
    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if (_repositories == null)
            _repositories = new Hashtable();

        var type = typeof(TEntity).Name;

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(GenericRepository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _context);
            _repositories.Add(type, repositoryInstance);
        }

        return (IGenericRepository<TEntity>)_repositories[type]!;
    }

    /// <summary>
    /// Tüm değişiklikleri tek seferde veritabanına yansıtır (Transaction Commit).
    /// Unit of Work deseninin en önemli parçasıdır. Her repository kendi içinde SaveChanges yapmaz,
    /// işlemler toplu olarak bu metod üzerinden yapılır.
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// IDisposable arayüzünün implementasyonu.
    /// DbContext gibi yönetilmeyen (unmanaged) kaynakların işimiz bittiğinde 
    /// bellekten temizlenmesini (garbage collection beklemeden) sağlar.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }
}
