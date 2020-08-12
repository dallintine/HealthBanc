using AutoMapper;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.Infrastructure.Mail;
using HealthBanc.Request;
using HealthBanc.Response;
using HealthBanc.Services.ImageService;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static HealthBanc.Infrastructure.Mail.EmailSender;

namespace HealthBanc.Controllers.BackendAdmin
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IMapper _mapper;
        private readonly INotificationRepository _notificationRepository;
        private readonly IImageService _imageService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(IEmailSender emailSender,IMapper mapper,INotificationRepository notificationRepository,IImageService imageService,ILogger<NotificationController>logger)
        {
            _emailSender = emailSender;
            _mapper = mapper;
            _notificationRepository = notificationRepository;
            _imageService = imageService;
            _logger = logger;
        }

        //WORKING1
        /// <summary>
        /// Send Helium Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public IActionResult SendHeliumNotification(HeliumHealthCollectionViewModel heliumHealth)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var @object = _mapper.Map<HelloEmail>(heliumHealth);
                    _emailSender.SendEmailWithObject("Oluwaseunayo.Lojede@sterling.ng", "d-ae6ac5d73c714e3896c7011b5276c2b5", @object);
                    return Ok(new ResponseMessage{ Status = true, Message = "Notification was sent successfully" });
                }
                catch(Exception ex)
                {
                    return BadRequest(new ResponseMessage{ Message = "An error occurred while trying to send notification" });
                }               
            }
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage(){ Data = errors });
        }

        //WORKING1
        /// <summary>
        /// Create Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        [Authorize]
        public async Task<IActionResult> CreateNotification([FromForm]NotificationViewModel notificationViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var image = "";
                    if (notificationViewModel.Image != null)
                    {
                        image = await _imageService.UploadPics("noificationimages", notificationViewModel.Image);
                    }
                    var notification = _mapper.Map<Notification>(notificationViewModel);
                    notification.ImageURl = image;
                    _notificationRepository.Create(notification);
                    await _notificationRepository.Save();
                    return Ok(new ResponseMessage { Message = "Notification was created successfully", Status = true });
                }
                catch(Exception ex)
                {
                    _logger.LogCritical("An error occurred while trying to create notification " + ex);
                    return BadRequest(new ResponseMessage { Message = "An error occurred while trying to create notification" });
                }
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors,Message="There were validation errors"});
        }


        //WORKING1
        /// <summary>
        /// Change notification status
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize]
        public async Task<IActionResult> ChangeNotificationStatus([FromQuery] int Id,int statusId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationById(Id);
                if (notification != null)
                {
                    notification.Status = statusId;
                    _notificationRepository.Update(notification);
                    await _notificationRepository.Save();
                    return Ok(new ResponseMessage {Status=true,Message="Status was changed successfully"});
                }
                return NotFound(new ResponseMessage { Message="Notification was not found"});
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to change notification status " + ex);
                return BadRequest(new ResponseMessage {Message= "An error occurred while trying to change notification status"});
            }           
        }

        //WORKING1
        /// <summary>
        /// Get paged Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<Notification>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize]
        public async Task<IActionResult> GetAllNotification([FromQuery] PaginationQuery paginationQuery)
        {
            try
            {
                if (paginationQuery.Status < 1 || paginationQuery.Status > 4)
                {
                    return BadRequest(new ResponseMessage { Message = "Status value is invalid" });
                }
                var notifications = await _notificationRepository.GetAllNotifications(paginationQuery);

                notifications.PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null;
                notifications.PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null;
                return Ok(new ResponseMessage<PagedResponse<Notification>> { Data= notifications, Message="Notification was fecthed successfully",Status= true });
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get all notifications " + ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get all notifications" });
            }
        }

        //WORKING1
        /// <summary>
        /// Get paged Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<Notification>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize]
        public async Task<IActionResult> GetNotification([FromQuery]int Id)
        {
            if(Id < 1)
            {
                return BadRequest(new ResponseMessage { Message = "Id can not be less than 1" });
            }
            try
            {
                var notification = await _notificationRepository.GetNotificationById(Id);
                if (notification is null) return NotFound(new ResponseMessage { Message = "Notification was not found" });
                return Ok(new ResponseMessage<Notification> { Data = notification, Status = true, Message = "Notification was fetched successfully" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get notification " + ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get notification" });
            }           
        }

        //WORKING1
        /// <summary>
        /// Move notification to trash
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize]
        public async Task<IActionResult> TrashNotification([FromQuery] int Id)
        {
            if (Id < 1)
            {
                return BadRequest(new ResponseMessage { Message = "Id can not be less than 1" });
            }
            try
            {
                var notification = await _notificationRepository.GetNotificationById(Id);
                if (notification is null) return NotFound(new ResponseMessage { Message = "Notification was not found" });
                notification.Status = 4;
                _notificationRepository.Update(notification);
                await _notificationRepository.Save();
                return Ok(new ResponseMessage<Notification> { Data = notification, Status = true, Message = "Notification was trashed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to trash notification " + ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to trash notification " });
            }
        }

        //WORKING1
        /// <summary>
        /// Delete notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize]
        public async Task<IActionResult> DeleteNotification([FromQuery] int Id)
        {
            if (Id < 1)
            {
                return BadRequest(new ResponseMessage { Message = "Id can not be less than 1" });
            }
            try
            {
                var notification = await _notificationRepository.GetNotificationById(Id);
                if (notification is null) return NotFound(new ResponseMessage { Message = "Notification was not found" });
                _notificationRepository.Delete(notification);
                await _notificationRepository.Save();
                return Ok(new ResponseMessage<Notification> { Data = notification, Status = true, Message = "Notification was deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to deleted notification " + ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to deleted notification " });
            }
        }
    }
}