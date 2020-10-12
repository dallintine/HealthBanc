using AutoMapper;
using Hangfire;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.Infrastructure.Mail;
using HealthBanc.Request;
using HealthBanc.Response;
using HealthBanc.Services.AuditAndReport.AuditLog;
using HealthBanc.Services.ImageService;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        private readonly AuditLogService _auditLogServices;
        private readonly IBackendAdminRepository _adminRepository;

        public NotificationController(IEmailSender emailSender,IMapper mapper,INotificationRepository notificationRepository,IImageService imageService,ILogger<NotificationController>logger,
            AuditLogService auditLogServices, IBackendAdminRepository adminRepository)
        {
            _emailSender = emailSender;
            _mapper = mapper;
            _notificationRepository = notificationRepository;
            _imageService = imageService;
            _logger = logger;
            _auditLogServices = auditLogServices;
            _adminRepository = adminRepository;
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
                var @object = _mapper.Map<HelloEmail>(heliumHealth);
                _emailSender.SendEmailWithObject("Oluwaseunayo.Lojede@sterling.ng", "d-ae6ac5d73c714e3896c7011b5276c2b5", @object);
                return Ok(new ResponseMessage{ Status = true, Message = "Notification was sent successfully" });                             
            }
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage(){ Data = errors, Message=errors.FirstOrDefault()});
        }

        //WORKING1
        /// <summary>
        /// Create Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> CreateNotification([FromForm]NotificationViewModel notificationViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                string userMail = User.FindFirst(ClaimTypes.Email)?.Value;
                var backedAdmin = await _adminRepository.GetAdminByEmail(userMail);

                var image = "";
                if (notificationViewModel.Image != null)
                {
                    image = await _imageService.UploadPics("noificationimages", notificationViewModel.Image);
                }
                var notification = _mapper.Map<Notification>(notificationViewModel);
                notification.ImageURl = image;
                _notificationRepository.Create(notification);
                await _notificationRepository.Save();

                var auditViewModel = new AdminAuditLogViewModel(Id,backedAdmin.Id, null, null, "Created Notification", $"Notification with ID {notification.Id} was created");
                BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                return Ok(new ResponseMessage { Message = "Notification was created successfully", Status = true });
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
            return BadRequest(new ResponseMessage { Data = errors,Message=errors.FirstOrDefault()});
        }

        //WORKING1
        /// <summary>
        /// Restore Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> RestoreNotification(int notificationId)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            string userMail = User.FindFirst(ClaimTypes.Email)?.Value;
            var backedAdmin = await _adminRepository.GetAdminByEmail(userMail);

            var notification = await _notificationRepository.GetNotificationById(notificationId);
            if (notification != null)
            {
                notification.Status = notification.RestoreServiceId??notification.Status;
                notification.RestoreServiceId = null;
                _notificationRepository.Update(notification);
                await _notificationRepository.Save();

                var auditViewModel = new AdminAuditLogViewModel(Id,backedAdmin.Id, null, null, "Restore Notification", $"Notification with ID {notification.Id} was restored");
                BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                return Ok(new ResponseMessage {Message="Notification was changed successfully", Status=true });
            }
            return NotFound(new ResponseMessage { Message = "Notification was not found"});
        }


        //WORKING1
        /// <summary>
        /// Change notification status
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> ChangeNotificationStatus([FromQuery] int Id,int statusId)
        {
            if (statusId < 1 || statusId > 3) return BadRequest(new ResponseMessage { Message = "statusId is invalId" });
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

        //WORKING1
        /// <summary>
        /// Get paged Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<Notification>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> GetAllNotification([FromQuery] PaginationQuery paginationQuery)
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

        //WORKING1
        /// <summary>
        /// Get specific Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<Notification>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> GetNotification([FromQuery]int Id)
        {
            if(Id < 1)
            {
                return BadRequest(new ResponseMessage { Message = "Id can not be less than 1" });
            }
            var notification = await _notificationRepository.GetNotificationById(Id);
            if (notification is null) return NotFound(new ResponseMessage { Message = "Notification was not found" });
            return Ok(new ResponseMessage<Notification> { Data = notification, Status = true, Message = "Notification was fetched successfully" });
        }

        //WORKING1
        /// <summary>
        /// Move notification to trash
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> TrashNotification([FromQuery] int Id)
        {
            if (Id < 1)
            {
                return BadRequest(new ResponseMessage { Message = "Id can not be less than 1"});
            }
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int adminId = int.Parse(userId);

            string userMail = User.FindFirst(ClaimTypes.Email)?.Value;
            var backedAdmin = await _adminRepository.GetAdminByEmail(userMail);

            var notification = await _notificationRepository.GetNotificationById(Id);
            if (notification is null) return NotFound(new ResponseMessage { Message = "Notification was not found" });
            notification.RestoreServiceId = notification.Status;
            notification.Status = 4;
            _notificationRepository.Update(notification);
            await _notificationRepository.Save();

            var auditViewModel = new AdminAuditLogViewModel(adminId,backedAdmin.Id, null, null, "Trash Notification", $"Notification with ID {notification.Id} was trash");
            BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

            return Ok(new ResponseMessage<Notification> { Data = notification, Status = true, Message = "Notification was trashed successfully" });
        }

        //WORKING1
        /// <summary>
        /// Delete notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> DeleteNotification([FromQuery] int Id)
        {
            if (Id < 1)
            {
                return BadRequest(new ResponseMessage { Message = "Id can not be less than 1" });
            }
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int adminId = int.Parse(userId);

            string userMail = User.FindFirst(ClaimTypes.Email)?.Value;
            var backedAdmin = await _adminRepository.GetAdminByEmail(userMail);

            var notification = await _notificationRepository.GetNotificationById(Id);
            if (notification is null) return NotFound(new ResponseMessage { Message = "Notification was not found" });
            _notificationRepository.Delete(notification);
            await _notificationRepository.Save();

            var auditViewModel = new AdminAuditLogViewModel(adminId,backedAdmin.Id, null, null, "Deleted Notification", $"Notification with ID {notification.Id} was deleted");
            BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

            return Ok(new ResponseMessage<Notification> { Data = notification, Status = true, Message = "Notification was deleted successfully" });
        }
    }
}