using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Entities;
using AutoMapper;

namespace AuthenticationService.Core.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.IsBanned, opt => opt.MapFrom(src => 
                src.IsBanned && (!src.BannedUntil.HasValue || src.BannedUntil.Value >= DateTime.UtcNow)));
                
        CreateMap<User, AuthorApplicationDto>();
        CreateMap<UserNotification, UserNotificationDto>();
    }
}
