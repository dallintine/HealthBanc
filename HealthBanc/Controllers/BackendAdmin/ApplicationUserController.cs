using AutoMapper;
using HealthBanc.DataAccess.Implementation;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.ApplicationUserDTOs;
using HealthBanc.Request;
using HealthBanc.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.BackendAdmin
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class ApplicationUserController : ControllerBase
    {
        private readonly IApplicationUserRepository _applicationUserRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ApplicationUserController> _logger;

        public ApplicationUserController(IApplicationUserRepository applicationUserRepository,IMapper mapper,ILogger<ApplicationUserController> logger)
        {
            _applicationUserRepository = applicationUserRepository;
            _mapper = mapper;
            _logger = logger;
        }

        //WORKING1
        /// <summary>
        /// Get All Users
        /// </summary>
        [ProducesResponseType(200, Type = typeof(PagedResponse<ApplicationUserDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllUsers([FromQuery]PaginationQuery paginationQuery)
        {
            try
            {
                var users = await _applicationUserRepository.GetAllUsers(paginationQuery);
                var userDT0 = _mapper.Map<IEnumerable<ApplicationUser>, List<ApplicationUserDTO>>(users.Data);

                var paginatedResponse = new PagedResponse<ApplicationUserDTO>
                {
                    Data = userDT0,
                    PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                    PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                    RecordCount = users.RecordCount,
                    PageCount = users.PageCount
                };
                return Ok(paginatedResponse);
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get all users " + ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get all users" });
            }
        }
    }
}
