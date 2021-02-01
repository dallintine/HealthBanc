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
        }
    }
}
