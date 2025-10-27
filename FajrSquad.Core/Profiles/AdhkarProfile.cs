using AutoMapper;
using FajrSquad.Core.DTOs.Adhkar;
using FajrSquad.Core.Entities.Adhkar;

namespace FajrSquad.Core.Profiles
{
    public class AdhkarProfile : Profile
    {
        public AdhkarProfile()
        {
            CreateMap<Adhkar, AdhkarDto>();
            CreateMap<AdhkarText, AdhkarTextDto>();
            CreateMap<AdhkarSet, AdhkarSetDto>();
            CreateMap<AdhkarSetItem, AdhkarSetItemDto>();
            CreateMap<UserAdhkarProgress, UserAdhkarProgressDto>();
            CreateMap<UserAdhkarBookmark, UserAdhkarBookmarkDto>();
        }
    }

    public class UserAdhkarBookmarkDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AdhkarId { get; set; }
        public string? Note { get; set; }
    }
}
