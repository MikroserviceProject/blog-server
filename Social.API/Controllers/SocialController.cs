using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.CORE.DTOs;
using Social.CORE.Interfaces;

namespace Social.API.Controllers;

[ApiController]
[Route("api/social")]
public class SocialController : ControllerBase
{
    private readonly ISocialService _socialService;

    public SocialController(ISocialService socialService)
    {
        _socialService = socialService;
    }

    [Authorize]
    [HttpPost("follow/{followingId}")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FollowUser(Guid followingId)
    {
        var followerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(followerIdClaim) || !Guid.TryParse(followerIdClaim, out var followerId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        var result = await _socialService.FollowUserAsync(followerId, followingId);
        if (!result.Success) return BadRequest(result);
        
        return Ok(result);
    }

    [Authorize]
    [HttpPost("unfollow/{followingId}")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnfollowUser(Guid followingId)
    {
        var followerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(followerIdClaim) || !Guid.TryParse(followerIdClaim, out var followerId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        var result = await _socialService.UnfollowUserAsync(followerId, followingId);
        if (!result.Success) return BadRequest(result);
        
        return Ok(result);
    }

    [HttpGet("stats/{userId}")]
    [ProducesResponseType(typeof(ApiResponseDto<SocialStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(Guid userId)
    {
        var result = await _socialService.GetUserStatsAsync(userId);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("check-follow/{followingId}")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckFollowStatus(Guid followingId)
    {
        var followerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(followerIdClaim) || !Guid.TryParse(followerIdClaim, out var followerId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        var result = await _socialService.CheckFollowStatusAsync(followerId, followingId);
        return Ok(result);
    }

    [HttpGet("followers-ids/{userId}")]
    [ProducesResponseType(typeof(ApiResponseDto<List<Guid>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowersIds(Guid userId)
    {
        var result = await _socialService.GetFollowerIdsAsync(userId);
        return Ok(result);
    }

    [HttpGet("following-ids/{userId}")]
    [ProducesResponseType(typeof(ApiResponseDto<List<Guid>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowingIds(Guid userId)
    {
        var result = await _socialService.GetFollowingIdsAsync(userId);
        return Ok(result);
    }
}
