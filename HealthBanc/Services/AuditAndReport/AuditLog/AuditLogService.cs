using AutoMapper;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models.ReportAndLogs;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAParser;

namespace HealthBanc.Services.AuditAndReport.AuditLog
{
    public class AuditLogService
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IUserAuditLogRepository _userAuditLog;
        private readonly IMapper _mapper;

        public AuditLogService(IHttpContextAccessor accessor,IUserAuditLogRepository userAuditLog,IMapper mapper)
        {
            _accessor = accessor;
            _userAuditLog = userAuditLog;
            _mapper = mapper;
        }

        public async Task UserCreateAuditLog(AuditLogViewModel viewModel)
        {
            var auditLog = _mapper.Map<UserAuditLog>(viewModel);
            auditLog.IPAddress = GetIPAddress();
            auditLog.Device = GetDevice();
            auditLog.Date = DateTime.Now;
            auditLog.Id = new Guid();
            _userAuditLog.Create(auditLog);
            await _userAuditLog.Save();
            await Task.CompletedTask;
        }

        public string GetIPAddress()
        {
            var ip = /*_accessor.HttpContext.Connection.RemoteIpAddress.ToString();*/ "test";
            return ip;
        }

        public string GetDevice()
        {
            var userAgent = _accessor.HttpContext.Request.Headers["User-Agent"];
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);

            return  c.OS.ToString() + " " + c.UA.ToString() + " " +c.Device.Brand+","+c.Device.Family;
        }
    }
}
