namespace Social.CORE.DTOs;

public class SocialStatsDto
{
    public Guid UserId { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}
