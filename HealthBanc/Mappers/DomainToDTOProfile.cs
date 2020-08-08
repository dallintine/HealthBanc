using AutoMapper;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.ApplicationUserDTOs;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Mappers
{
    public class DomainToDTOProfile : Profile
    {
        public DomainToDTOProfile()
        {
            CreateMap<Service, ServiceBreakdown>();
            CreateMap<ApplicationUser, ApplicationUserDTO>();
        }
    }
}
