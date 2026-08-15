using Microsoft.EntityFrameworkCore;
using Social.CORE.DTOs;
using Social.CORE.Entities;
using Social.CORE.Interfaces;
using Social.INFRASTRUCTURE.Data;

namespace Social.INFRASTRUCTURE.Repositories;

public class SocialService : ISocialService
{
    private readonly SocialDbContext _context;

    public SocialService(SocialDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponseDto<bool>> FollowUserAsync(Guid followerId, Guid followingId)
    {
        if (followerId == followingId)
            return ApiResponseDto<bool>.Fail("Kendinizi takip edemezsiniz.");

        var exists = await _context.FollowRelations
            .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        if (exists)
            return ApiResponseDto<bool>.Fail("Bu kullanıcıyı zaten takip ediyorsunuz.");

        var follow = new FollowRelation
        {
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        };

        _context.FollowRelations.Add(follow);
        await _context.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Kullanıcı başarıyla takip edildi.");
    }

    public async Task<ApiResponseDto<bool>> UnfollowUserAsync(Guid followerId, Guid followingId)
    {
        var relation = await _context.FollowRelations
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        if (relation == null)
            return ApiResponseDto<bool>.Fail("Bu kullanıcıyı zaten takip etmiyorsunuz.");

        _context.FollowRelations.Remove(relation);
        await _context.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Takipten çıkıldı.");
    }

    public async Task<ApiResponseDto<SocialStatsDto>> GetUserStatsAsync(Guid userId)
    {
        var followers = await _context.FollowRelations.CountAsync(f => f.FollowingId == userId);
        var following = await _context.FollowRelations.CountAsync(f => f.FollowerId == userId);

        var stats = new SocialStatsDto
        {
            UserId = userId,
            FollowersCount = followers,
            FollowingCount = following
        };

        return ApiResponseDto<SocialStatsDto>.Ok(stats);
    }

    public async Task<ApiResponseDto<bool>> CheckFollowStatusAsync(Guid followerId, Guid followingId)
    {
        var exists = await _context.FollowRelations
            .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        return ApiResponseDto<bool>.Ok(exists);
    }

    public async Task<ApiResponseDto<List<Guid>>> GetFollowerIdsAsync(Guid userId)
    {
        var ids = await _context.FollowRelations
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId)
            .ToListAsync();
            
        return ApiResponseDto<List<Guid>>.Ok(ids);
    }

    public async Task<ApiResponseDto<List<Guid>>> GetFollowingIdsAsync(Guid userId)
    {
        var ids = await _context.FollowRelations
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync();
            
        return ApiResponseDto<List<Guid>>.Ok(ids);
    }
}
