using Microsoft.EntityFrameworkCore;
using BlogSite.CORE.Data;
using BlogSite.CORE.Entities;
using BlogSite.CORE.Enums;
using BlogSite.CORE.Repositories.Abstract;

namespace BlogSite.CORE.Repositories.Concrete
{
    public class PostRepository : GenericRepository<Post>, IPostRepository
    {
        public PostRepository(BlogDbContext context) : base(context) { }

        public async Task<(List<Post> Items, int TotalCount)> GetAllAsync(
            PostStatus? status, PostType? type, Guid? authorId, string? search, string? tag, int? page, int? pageSize)
        {
            var query = _context.Posts.AsQueryable();

            if (status.HasValue) query = query.Where(p => p.Status == status.Value);
            if (type.HasValue) query = query.Where(p => p.Type == type.Value);
            if (authorId.HasValue && authorId.Value != Guid.Empty) query = query.Where(p => p.AuthorId == authorId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"%{search.Trim()}%";
                query = query.Where(p => EF.Functions.ILike(p.Title, term) || EF.Functions.ILike(p.Content, term));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var lowerTag = tag.Trim().ToLower();
                query = query.Where(p => p.Tags.Any(t => t.ToLower() == lowerTag));
            }

            query = query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
            var totalCount = await query.CountAsync();

            if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
                query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);

            var items = await query.ToListAsync();
            return (items, totalCount);
        }
    }
}
