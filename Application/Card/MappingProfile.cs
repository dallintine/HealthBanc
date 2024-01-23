using Application.Card.DTO;
using AutoMapper;

namespace Application.Card
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Domain.Entities.Card, CardDTO>();
        }
    }
}
