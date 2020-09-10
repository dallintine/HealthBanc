using AutoMapper;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.ApplicationUserDTOs;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.Request.Tokenize;
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
            CreateMap<AxaMansardUserProfile, AxaMansardUserDTO>();
            CreateMap<AxaMansardUserProfile, SubscribePayment>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(x => x.Othernames))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(x => x.Surname))
                .ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(x => x.Email))
                .ForMember(dest => dest.RepaymentAmount, opt => opt.MapFrom(x => Convert.ToInt32(x.Premium)))
                .ForMember(dest => dest.Channel, opt => opt.MapFrom(x => "healthinsured"))
                .ForMember(dest => dest.TokenType, opt => opt.MapFrom(x => "paystack"))
                .ForMember(dest => dest.NextRepaymentDate, opt => opt.MapFrom(x => DateTime.Now.AddMonths(1)));
        }
    }
}