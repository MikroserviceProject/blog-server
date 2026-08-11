using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogSite.CORE.Dtos;
using BlogSite.CORE.Enums;
using BlogSite.CORE.Services;

namespace BlogSite.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxPhotoSizeBytes = 5 * 1024 * 1024; // 5 MB

        private readonly IPostService _postService;
        private readonly IWebHostEnvironment _environment;

        public PostsController(IPostService postService, IWebHostEnvironment environment)
        {
            _postService = postService;
            _environment = environment;
        }

        // GET: api/posts?status=Published&type=Blog&authorId=...&search=...
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostResponseDto>>> GetPosts(
            [FromQuery] PostStatus? status,
            [FromQuery] PostType? type,
            [FromQuery] Guid? authorId,
            [FromQuery] string? search)
        {
            var items = await _postService.GetPostsAsync(status, type, authorId, search);
            return Ok(items);
        }

        // GET: api/posts/paged?status=Published&type=Blog&search=...&page=1&pageSize=9
        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponseDto<PagedResultDto<PostResponseDto>>>> GetPagedPosts(
            [FromQuery] PostStatus? status,
            [FromQuery] PostType? type,
            [FromQuery] Guid? authorId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 9)
        {
            var result = await _postService.GetPagedPostsAsync(status, type, authorId, search, page, pageSize);
            return Ok(ApiResponseDto<PagedResultDto<PostResponseDto>>.Ok(result));
        }

        // GET: api/posts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PostResponseDto>> GetPost(int id)
        {
            var post = await _postService.GetPostAsync(id);
            return post == null ? NotFound() : Ok(post);
        }

        // POST: api/posts
        [Authorize(Roles = "Admin,Author")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<PostResponseDto>> CreatePost([FromForm] CreatePostDto dto, IFormFile? photo)
        {
            string? photoUrl = null;
            if (photo != null)
            {
                var (url, error) = await TrySavePhotoAsync(photo);
                if (error != null)
                    return BadRequest(error);

                photoUrl = url;
            }

            var created = await _postService.CreatePostAsync(dto, GetCurrentUserId(), photoUrl);
            return CreatedAtAction(nameof(GetPost), new { id = created.Id }, created);
        }

        // PUT: api/posts/5
        [Authorize(Roles = "Admin,Author")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto dto, IFormFile? photo)
        {
            string? photoUrl = null;
            if (photo != null)
            {
                var (url, error) = await TrySavePhotoAsync(photo);
                if (error != null)
                    return BadRequest(error);

                photoUrl = url;
            }

            var updated = await _postService.UpdatePostAsync(id, dto, photoUrl);
            return updated == null ? NotFound() : Ok(updated);
        }

        // PUT: api/posts/5/with-photo
        [Authorize(Roles = "Admin,Author")]
        [HttpPut("{id}/with-photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePostWithPhoto(int id, [FromForm] UpdatePostDto dto, IFormFile? photo)
        {
            string? photoUrl = null;
            if (photo != null)
            {
                var (url, error) = await TrySavePhotoAsync(photo);
                if (error != null)
                    return BadRequest(error);

                photoUrl = url;
            }

            var updated = await _postService.UpdatePostWithPhotoAsync(id, dto, photoUrl);
            return updated == null ? NotFound() : Ok(updated);
        }

        // DELETE: api/posts/5
        [Authorize(Roles = "Admin,Author")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var deleted = await _postService.DeletePostAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        // POST: api/posts/5/admin-delete
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/admin-delete")]
        public async Task<IActionResult> AdminDeletePost(int id, [FromBody] AdminDeletePostDto dto)
        {
            var deletedTitle = await _postService.AdminDeletePostAsync(id);
            if (deletedTitle == null)
                return NotFound(new { message = "Yazı bulunamadı." });

            return Ok(new { success = true, message = $"'{deletedTitle}' başlıklı yazı kaldırıldı." });
        }

        private Guid GetCurrentUserId()
        {
            // MapInboundClaims ayarına bağlı olarak claim ismi "nameid" (kısa) veya tam
            // ClaimTypes.NameIdentifier URI'si olarak gelebilir; ikisini de kontrol ediyoruz.
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid")
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
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
    }
}
