using Application.ViewModels;
using AutoMapper;
using Domain.Models;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Mappers
{
    public class ViewModelToDomainProfile : Profile
    {
        public ViewModelToDomainProfile()
        {
            CreateMap<NotificationViewModel, Notification>();
            CreateMap<AuditLogViewModel, UserAuditLog>();
            CreateMap<AdminAuditLogViewModel, AdminAuditLog>();
            CreateMap<HealthFinanceCollectionViewModel, HealthFinance>()
                .ForMember( dest => dest.DateSubmitted, opt => opt.MapFrom(x => DateTime.Now))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(x => decimal.Parse(x.Amount)));
        }
    }
}
