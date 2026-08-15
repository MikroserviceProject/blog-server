using Microsoft.EntityFrameworkCore;
using Social.CORE.Entities;

namespace Social.INFRASTRUCTURE.Data;

public class SocialDbContext : DbContext
{
    public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options)
    {
    }

    public DbSet<FollowRelation> FollowRelations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FollowRelation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Kullanıcı aynı kişiyi birden fazla kez takip edemez
            entity.HasIndex(e => new { e.FollowerId, e.FollowingId }).IsUnique();
        });
    }
}
