using Application.DTO;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ExceptionLog;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.AuditAndReport
{
    public class Activity_ErrorLogService
    {
        private readonly IRepositoryWrapper _repoWrapper;

        public Activity_ErrorLogService(IRepositoryWrapper repoWrapper)
        {
            _repoWrapper = repoWrapper;
        }

        public async Task<ResponseMessage> GetPaginatedErrorLog(PaginationQuery paginationQuery)
        {
            var errorLogs = await _repoWrapper.ExceptionLog.GetPaginatedErrorLog(paginationQuery);

            var paginatedResponse = new PagedResponse<ExceptionLog>
            {
                Data = errorLogs.Data,
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = errorLogs.RecordCount,
                PageCount = errorLogs.PageCount
            };
            return new ResponseMessage { Data = paginatedResponse, Status = true };
        }

        public async Task<ResponseMessage> GetLoggedInUserHealthInsuredPaginatedActivityLog(PaginationQuery paginationQuery, int userId)
        {
            var individualUser = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (individualUser is null)
            {
                var corporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                if (!(corporateUser is null))
                {
                    var corporateUserActivityLog = await _repoWrapper.HealthInsuredActivityLog.GetPaginatedActivityLogByProfileId(paginationQuery, null, corporateUser.Id, ServiceNames.HealthInsured.ToString());
                    var paginatedResponse = new PagedResponse<ActivityLog>
                    {
                        Data = corporateUserActivityLog.Data,
                        PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                        PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                        RecordCount = corporateUserActivityLog.RecordCount,
                        PageCount = corporateUserActivityLog.PageCount
                    };
                    return new ResponseMessage { Data = paginatedResponse, Status = true };
                }
                return new ResponseMessage { Status = false, Message = "User activity log does not exist" };
            }
            var individualUserActivityLog = await _repoWrapper.HealthInsuredActivityLog.GetPaginatedActivityLogByProfileId(paginationQuery, individualUser.Id, null, ServiceNames.HealthInsured.ToString());
            var response = new PagedResponse<ActivityLog>
            {
                Data = individualUserActivityLog.Data,
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = individualUserActivityLog.RecordCount,
                PageCount = individualUserActivityLog.PageCount
            };
            return new ResponseMessage { Data = response, Status = true };
        }

        public async Task<ResponseMessage> GetHealthInsuredPaginatedActivityLogByEmail(PaginationQuery paginationQuery, string email)
        {
            var individualUser = await _repoWrapper.InsuranceProfile.GetByEmail(email);
            if (individualUser is null)
            {
                var corporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(email);
                if (!(corporateUser is null))
                {
                    var corporateUserActivityLog = await _repoWrapper.HealthInsuredActivityLog.GetPaginatedActivityLogByProfileId(paginationQuery, null, corporateUser.Id, ServiceNames.HealthInsured.ToString());
                    var paginatedResponse = new PagedResponse<ActivityLog>
                    {
                        Data = corporateUserActivityLog.Data,
                        PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                        PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                        RecordCount = corporateUserActivityLog.RecordCount,
                        PageCount = corporateUserActivityLog.PageCount
                    };
                    return new ResponseMessage { Data = paginatedResponse, Status = true };
                }
                return new ResponseMessage { Status = false, Message = "User activity log does not exist" };
            }
            var individualUserActivityLog = await _repoWrapper.HealthInsuredActivityLog.GetPaginatedActivityLogByProfileId(paginationQuery, individualUser.Id, null, ServiceNames.HealthInsured.ToString());
            var response = new PagedResponse<ActivityLog>
            {
                Data = individualUserActivityLog.Data,
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = individualUserActivityLog.RecordCount,
                PageCount = individualUserActivityLog.PageCount
            };
            return new ResponseMessage { Data = response, Status = true };
        }
    }
}
