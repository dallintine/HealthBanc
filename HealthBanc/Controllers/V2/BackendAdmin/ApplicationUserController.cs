using Application.DTO;
using Application.Interfaces;
using Application.Services;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using DataAccess.DTO.ApplicationUserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.V2.BackendAdmin
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class ApplicationUserController : ControllerBase
    {
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IMapper _mapper;
        private readonly ILogger<ApplicationUserController> _logger;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        private readonly ResponseHelper _responseHelper;

        public ApplicationUserController(IRepositoryWrapper repoWrapper, IMapper mapper, ILogger<ApplicationUserController> logger, IEncryptAndDecrypt encryptDecrypt,
            ResponseHelper responseHelper)
        {
            _repoWrapper = repoWrapper;
            _mapper = mapper;
            _logger = logger;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
        }

        //WORKING1
        /// <summary>
        /// Get All Users and filter based on service Used    
        /// </summary>
        [ProducesResponseType(200, Type = typeof(PagedResponse<ApplicationUserDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetAllUsers(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            var users = await _repoWrapper.ApplicationUser.GetAllUsers(paginationQuery);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(users));
            return Ok(data);
        }

        //WORKING1
        /// <summary>
        /// Get  User Detail
        /// </summary>
        [ProducesResponseType(200, Type = typeof(PagedResponse<ApplicationUserDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetUser(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var emailViewModel = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);
            var email = emailViewModel.Email;
            if (email != null)
            {
                var user = await _repoWrapper.ApplicationUser.GetByEmailAsync(email);
                var userDTO = _mapper.Map<ApplicationUserDTO>(user);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage<ApplicationUserDTO> { Data = userDTO, Status = true, Message = "User detail was fetched successfully" }));
                return Ok(data);
            }
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "email can not be null" }));
            return BadRequest(data2);
        }
    }
}
