using Microsoft.EntityFrameworkCore;
using BlogSite.CORE.Entities;

namespace BlogSite.CORE.Data
{
    public class BlogDbContext : DbContext
    {
        public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options) { }

        public DbSet<Post> Posts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>(entity =>
            {
                entity.Property(p => p.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Content)
                    .IsRequired();

                entity.Property(p => p.PhotoUrl)
                    .IsRequired(false);

                entity.Property(p => p.Type)
                    .HasConversion<string>();

                entity.Property(p => p.Status)
                    .HasConversion<string>();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
