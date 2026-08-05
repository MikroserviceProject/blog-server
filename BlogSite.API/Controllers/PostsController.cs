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
        private readonly BlogDbContext _context;

        public PostsController(BlogDbContext context)
        {
            _context = context;
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
        public async Task<ActionResult<PostResponseDto>> CreatePost(CreatePostDto dto)
        {
            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                PhotoUrl = dto.PhotoUrl
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, ToResponseDto(post));
        }

        // PUT: api/posts/5
        // [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, UpdatePostDto dto)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
                return NotFound();

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.Type = dto.Type;
            post.PhotoUrl = dto.PhotoUrl;
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
