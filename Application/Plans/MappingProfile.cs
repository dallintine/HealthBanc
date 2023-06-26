using Application.Plans.DTO;
using AutoMapper;
using Domain.Entities;
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
            CreateMap<Plan, PlanDTO>();
        }
    }
}
