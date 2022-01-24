using Application.DTO;
using AutoMapper;
using DataAccess.DTO.ApplicationUserDTOs;
using Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using Microsoft.Extensions.Options;
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
            CreateMap<DebitCard, CardDTO>();
        }
    }
}