using AutoMapper;
using BlogSite.CORE.Dtos;
using BlogSite.CORE.Entities;
using BlogSite.CORE.Enums;
using BlogSite.CORE.Repositories.Abstract;

namespace BlogSite.CORE.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _repository;
        private readonly IMapper _mapper;

        public PostService(IPostRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<PostResponseDto>> GetPostsAsync(
            PostStatus? status,
            PostType? type,
            Guid? authorId,
            string? search)
        {
            var (posts, _) = await _repository.GetAllAsync(status, type, authorId, search, null, null);
            return _mapper.Map<List<PostResponseDto>>(posts);
        }

        public async Task<PagedResultDto<PostResponseDto>> GetPagedPostsAsync(
            PostStatus? status,
            PostType? type,
            Guid? authorId,
            string? search,
            int page,
            int pageSize)
        {
            var (posts, totalCount) = await _repository.GetAllAsync(status, type, authorId, search, page, pageSize);

            return new PagedResultDto<PostResponseDto>
            {
                Items = _mapper.Map<List<PostResponseDto>>(posts),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PostResponseDto?> GetPostAsync(int id)
        {
            var post = await _repository.GetByIdAsync(id);
            return post == null ? null : _mapper.Map<PostResponseDto>(post);
        }

        public async Task<PostResponseDto> CreatePostAsync(CreatePostDto dto, Guid authorId, string? photoUrl)
        {
            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                Status = dto.Status,
                AuthorId = authorId,
                PhotoUrl = photoUrl
            };

            await _repository.AddAsync(post);
            await _repository.SaveChangesAsync();

            return _mapper.Map<PostResponseDto>(post);
        }

        public async Task<PostResponseDto?> UpdatePostAsync(int id, UpdatePostDto dto, string? newPhotoUrl)
        {
            var post = await _repository.GetByIdAsync(id);
            if (post == null) return null;

            if (newPhotoUrl != null)
                post.PhotoUrl = newPhotoUrl;
            else if (dto.RemovePhoto)
                post.PhotoUrl = null;

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.Type = post.PhotoUrl != null ? PostType.Blog : PostType.Koseyazisi;

            // Yayınlanmış bir yazı tekrar taslağa alınamaz — sadece Taslak -> Yayında yönünde geçiş serbest.
            var isDowngradeToDraft = post.Status == PostStatus.Published && dto.Status == PostStatus.Draft;
            if (!isDowngradeToDraft)
                post.Status = dto.Status;

            post.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();
            return _mapper.Map<PostResponseDto>(post);
        }

        public async Task<PostResponseDto?> UpdatePostWithPhotoAsync(int id, UpdatePostDto dto, string? newPhotoUrl)
        {
            var post = await _repository.GetByIdAsync(id);
            if (post == null) return null;

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.Type = dto.Type;
            post.Status = dto.Status;
            post.UpdatedAt = DateTime.UtcNow;

            if (newPhotoUrl != null)
                post.PhotoUrl = newPhotoUrl;
            else if (!string.IsNullOrEmpty(dto.PhotoUrl))
                post.PhotoUrl = dto.PhotoUrl;

            await _repository.SaveChangesAsync();
            return _mapper.Map<PostResponseDto>(post);
        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var post = await _repository.GetByIdAsync(id);
            if (post == null) return false;

            _repository.Remove(post);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<string?> AdminDeletePostAsync(int id)
        {
            var post = await _repository.GetByIdAsync(id);
            if (post == null) return null;

            var title = post.Title;
            _repository.Remove(post);
            await _repository.SaveChangesAsync();
            return title;
        }
    }
}
