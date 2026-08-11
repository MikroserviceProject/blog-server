using BlogSite.CORE.Entities;
using BlogSite.CORE.Enums;

namespace BlogSite.CORE.Repositories.Abstract
{
    public interface IPostRepository : IGenericRepository<Post>
    {
        Task<(List<Post> Items, int TotalCount)> GetAllAsync(
            PostStatus? status,
            PostType? type,
            Guid? authorId,
            string? search,
            int? page,
            int? pageSize);
    }
}
