using Application.ViewModels;
using AutoMapper;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.ReportAndLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAParser;

namespace Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog
{
    public class AuditLogService
    {
        private readonly IUserAuditLogRepository _userAuditLog;
        private readonly IMapper _mapper;
        private readonly IAdminAuditLogRepository _adminAuditLog;

        public AuditLogService(IUserAuditLogRepository userAuditLog,IMapper mapper, IAdminAuditLogRepository adminAuditLog)
        {
            _userAuditLog = userAuditLog;
            _mapper = mapper;
            _adminAuditLog = adminAuditLog;
        }

        public async Task UserCreateAuditLog(AuditLogViewModel viewModel, string ip, string device)
        {
            var auditLog = _mapper.Map<UserAuditLog>(viewModel);
            auditLog.IPAddress = ip;
            auditLog.Device = device;
            auditLog.Date = DateTime.Now;
            auditLog.Id = new Guid();
            _userAuditLog.Create(auditLog);
            await _userAuditLog.Save();
            await Task.CompletedTask;
        }

        public async Task AdminCreateAuditLog(AdminAuditLogViewModel viewModel)
        {
            var auditLog = _mapper.Map<AdminAuditLog>(viewModel);
            auditLog.IPAddress = "test";
            auditLog.Date = DateTime.Now;
            auditLog.Id = new Guid();
            _adminAuditLog.Create(auditLog);
            await _userAuditLog.Save();
            await Task.CompletedTask;
        }

        public string GetDevice(StringValues agent)
        {
            var userAgent = agent;
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);
            return  c.OS.ToString() + "," + c.UA.ToString() + "," +c.Device.Model+","+c.Device.Brand+","+c.Device.Family;
        }
    }
}
