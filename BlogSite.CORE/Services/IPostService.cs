using BlogSite.CORE.Dtos;
using BlogSite.CORE.Enums;

namespace BlogSite.CORE.Services
{
    public interface IPostService
    {
        Task<List<PostResponseDto>> GetPostsAsync(
            PostStatus? status,
            PostType? type,
            Guid? authorId,
            string? search,
            string? tag);

        Task<PagedResultDto<PostResponseDto>> GetPagedPostsAsync(
            PostStatus? status,
            PostType? type,
            Guid? authorId,
            string? search,
            string? tag,
            int page,
            int pageSize);

        Task<PostResponseDto?> GetPostAsync(int id);
        Task<PostResponseDto> CreatePostAsync(CreatePostDto dto, Guid authorId, string? photoUrl);
        Task<PostResponseDto?> UpdatePostAsync(int id, UpdatePostDto dto, string? newPhotoUrl);
        Task<PostResponseDto?> UpdatePostWithPhotoAsync(int id, UpdatePostDto dto, string? newPhotoUrl);
        Task<bool> DeletePostAsync(int id);

        /// <summary>Silinirse silinen postun başlığını, bulunamazsa null döner.</summary>
        Task<string?> AdminDeletePostAsync(int id);
    }
}
