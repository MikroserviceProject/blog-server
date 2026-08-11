using AutoMapper;
using BlogSite.CORE.Dtos;
using BlogSite.CORE.Entities;

namespace BlogSite.CORE.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Post, PostResponseDto>();
        }
    }
}
