using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogSite.CORE.Data;
using BlogSite.CORE.Dtos;
using BlogSite.CORE.Entities;
using BlogSite.CORE.Enums;

namespace BlogSite.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxPhotoSizeBytes = 5 * 1024 * 1024; // 5 MB

        private readonly BlogDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PostsController(BlogDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/posts?status=Published&type=Blog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostResponseDto>>> GetPosts(
            [FromQuery] PostStatus? status,
            [FromQuery] PostType? type)
        {
            var query = _context.Posts.AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (type.HasValue)
                query = query.Where(p => p.Type == type.Value);

            var posts = await query.ToListAsync();

            return Ok(posts.Select(ToResponseDto));
        }

        // GET: api/posts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PostResponseDto>> GetPost(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
                return NotFound();

            return Ok(ToResponseDto(post));
        }

        // POST: api/posts
        // [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<PostResponseDto>> CreatePost([FromForm] CreatePostDto dto, IFormFile? photo)
        {
            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                Status = dto.Status
            };

            if (photo != null)
            {
                var (photoUrl, error) = await TrySavePhotoAsync(photo);
                if (error != null)
                    return BadRequest(error);

                post.PhotoUrl = photoUrl;
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, ToResponseDto(post));
        }

        // PUT: api/posts/5
        // [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto dto, IFormFile? photo)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
                return NotFound();

            if (photo != null)
            {
                var (photoUrl, error) = await TrySavePhotoAsync(photo);
                if (error != null)
                    return BadRequest(error);

                post.PhotoUrl = photoUrl;
            }

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.Type = post.PhotoUrl != null ? PostType.Blog : PostType.Koseyazisi;
            post.Status = dto.Status;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/posts/5
        // [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
                return NotFound();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<(string? PhotoUrl, string? Error)> TrySavePhotoAsync(IFormFile file)
        {
            if (file.Length == 0)
                return (null, "Yüklenecek dosya boş.");

            if (file.Length > MaxPhotoSizeBytes)
                return (null, "Dosya boyutu 5 MB'ı geçemez.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedPhotoExtensions.Contains(extension))
                return (null, "Sadece jpg, jpeg, png, gif veya webp uzantılı resim dosyaları yüklenebilir.");

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "posts");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ($"/uploads/posts/{fileName}", null);
        }

        private static PostResponseDto ToResponseDto(Post post) => new()
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Type = post.Type,
            Status = post.Status,
            PhotoUrl = post.PhotoUrl,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }
}
