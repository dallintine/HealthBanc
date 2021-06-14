using Application.DTO;
using AutoMapper;
using DataAccess;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using HealthBanc.DTO.ApplicationUserDTOs;
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
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IMapper _mapper;
        private readonly ILogger<ApplicationUserController> _logger;

        public ApplicationUserController(IRepositoryWrapper repoWrapper,IMapper mapper,ILogger<ApplicationUserController> logger)
        {
            _repoWrapper = repoWrapper;
            _mapper = mapper;
            _logger = logger;
        }

        //WORKING1
        /// <summary>
        /// Get All Users and filter based on service Used    
        /// </summary>
        [ProducesResponseType(200, Type = typeof(PagedResponse<ApplicationUserDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllUsers([FromQuery]PaginationQuery paginationQuery)
        {
            var users = await _repoWrapper.ApplicationUser.GetAllUsers(paginationQuery);
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

        //WORKING1
        /// <summary>
        /// Get  User Detail
        /// </summary>
        [ProducesResponseType(200, Type = typeof(PagedResponse<ApplicationUserDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetUser([FromQuery]string email)
        {
            if(email != null)
            {
                var user = await _repoWrapper.ApplicationUser.GetByEmailAsync(email);
                var userDTO = _mapper.Map<ApplicationUserDTO>(user);
                return Ok(new ResponseMessage<ApplicationUserDTO> { Data = userDTO, Status = true, Message = "User detail was fetched successfully" });
            }
            return BadRequest(new ResponseMessage { Message = "email can not be null" });
        }
    }
}
