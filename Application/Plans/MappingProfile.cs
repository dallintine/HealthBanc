using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<PlanDTO, PlanDTO>();
        }
    }
}
