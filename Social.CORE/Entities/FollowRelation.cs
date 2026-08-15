namespace Social.CORE.Entities;

public class FollowRelation
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Takip eden kullanıcının ID'si (Authentication servisindeki User.Id)
    /// </summary>
    public Guid FollowerId { get; set; }
    
    /// <summary>
    /// Takip edilen kullanıcının ID'si (Authentication servisindeki User.Id)
    /// </summary>
    public Guid FollowingId { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
