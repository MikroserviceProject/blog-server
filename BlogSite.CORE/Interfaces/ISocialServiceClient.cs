using System.Text.Json;
using BlogSite.CORE.Dtos; // for some generic DTOs if needed

namespace BlogSite.CORE.Interfaces
{
    public interface ISocialServiceClient
    {
        Task<List<Guid>> GetFollowersAsync(Guid userId);
    }
}
