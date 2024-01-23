using Application.Common.DTO;
using Application.Common.Helpers;
using Application.Customer.Commands;
using Application.Customer.DTO;
using Application.Customer.Queries;
using Application.Image.DTO;
using Application.Service.Commands;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._00
{

    [ApiVersion("1.00")]
    public class CustomerController : BaseApiController
    {
        public CustomerController()
        {
        }

        /// <summary>
        /// Customer List
        /// </summary>
        /// <param name="queryList"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        [Cached(500)]
        [ProducesResponseType(200, Type = typeof(PageBaseResponse<List<CustomerDTO>>))]
        public async Task<IActionResult> PaginatedList([FromQuery] GetCustomerListQuery queryList)
        {
            return HandleResult(await Mediator.Send(queryList));
        }


        /// <summary>
        /// Get Customer Identification
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<CustomerIdDTO>))]
        public async Task<IActionResult> GetProfileIdentification([FromQuery] GetProfileIdQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }

        /// <summary>
        /// Get Customer Profile
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<CustomerProfileDTO>))]
        public async Task<IActionResult> GetProfile([FromQuery] GetCustomerProfileQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }

        /// <summary>
        /// Export List
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ExportList([FromQuery] ExportCustomerListQuery query)
        {
            var response = await Mediator.Send(query);
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = "CustomerList.xlsx";
            var content = response.Data as byte[];
            return File(content, contentType, fileName);
        }

        /// <summary>
        /// Update Customer Profile
        /// </summary>s
        /// <param name="profile"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateProfile(UpdateCustomerProfile profile)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(profile));
        }

        /// <summary>
        /// Update Profile Image
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<ImagesURLDTO>))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateProfileImage(UpdateProfileImage profile)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(profile));
        }
    }
}
