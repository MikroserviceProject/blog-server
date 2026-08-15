using Social.CORE.DTOs;

namespace Social.CORE.Interfaces;

public interface ISocialService
{
    Task<ApiResponseDto<bool>> FollowUserAsync(Guid followerId, Guid followingId);
    Task<ApiResponseDto<bool>> UnfollowUserAsync(Guid followerId, Guid followingId);
    Task<ApiResponseDto<SocialStatsDto>> GetUserStatsAsync(Guid userId);
    
    /// <summary>
    /// Returns true if followerId is following followingId
    /// </summary>
    Task<ApiResponseDto<bool>> CheckFollowStatusAsync(Guid followerId, Guid followingId);
    
    /// <summary>
    /// Returns the IDs of all users that are following the specified user
    /// </summary>
    Task<ApiResponseDto<List<Guid>>> GetFollowerIdsAsync(Guid userId);
    
    /// <summary>
    /// Returns the IDs of all users that the specified user is following
    /// </summary>
    Task<ApiResponseDto<List<Guid>>> GetFollowingIdsAsync(Guid userId);
}
