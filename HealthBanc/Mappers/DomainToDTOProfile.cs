using AutoMapper;
using HealthBanc.Domain.Models;
using HealthBanc.DTO;
using HealthBanc.DTO.ApplicationUserDTOs;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.DTO.TokenizationDTO;
using HealthBanc.Helpers;
using HealthBanc.Request.Tokenize;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Mappers
{
    public class DomainToDTOProfile : Profile
    {
        private Paystack Options { get; }

        public DomainToDTOProfile(IOptions<Paystack> optionAccessor)
        {
            Options = optionAccessor.Value;

            CreateMap<Service, ServiceBreakdown>();
            CreateMap<ApplicationUser, ApplicationUserDTO>();
            CreateMap<AxaMansardUserProfile, AxaMansardUserDTO>();
            CreateMap<AxaMansardBackgroundDTO, SubscribePayment>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(x => x.Othernames))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(x => x.Surname))
                .ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(x => x.Email))
                .ForMember(dest => dest.RepaymentAmount, opt => opt.MapFrom(x => Convert.ToInt32(x.Premium)))
                .ForMember(dest => dest.Channel, opt => opt.MapFrom(x => Options.Config.Channel))
                .ForMember(dest => dest.TokenType, opt => opt.MapFrom(x => Options.Config.TokenType));

            CreateMap<AxaMansardUserProfile, SubscribePayment>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(x => x.Othernames))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(x => x.Surname))
                .ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(x => x.Email))
                .ForMember(dest => dest.RepaymentAmount, opt => opt.MapFrom(x => Convert.ToInt32(x.Premium)))
                .ForMember(dest => dest.Channel, opt => opt.MapFrom(x => Options.Config.Channel))
                .ForMember(dest => dest.TokenType, opt => opt.MapFrom(x => Options.Config.TokenType))
                .ForMember(dest => dest.NextRepaymentDate, opt => opt.MapFrom(x => DateTime.Now.AddMonths(1)));



            CreateMap<DebitCard, CardDTO>();
            CreateMap<AxaMansardUserProfile, InactiveUsersDTO>()
                .ForMember(dest => dest.Premium, opt => opt.MapFrom(x => x.Premium.ToString()))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(x => x.DateOfBirth.ToString()));

            CreateMap<AxaMansardUserProfile, AxaMansardBackgroundDTO>();
        }
    }
}