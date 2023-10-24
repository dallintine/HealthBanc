using Application.Common.DTO;
using Application.Service.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service
{
    public class ServiceCommandHandler : IRequestHandler<CreateServiceCommand, BaseResponse>,
    IRequestHandler<UpdateServiceCommand, BaseResponse>,
    IRequestHandler<DeleteServiceCommand, BaseResponse>
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceCommandHandler> _logger;

        public ServiceCommandHandler(ApplicationDbContext context, ILogger<ServiceCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponse> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Edit Service request processing [Payload : {JsonConvert.SerializeObject(request)}] \n");
            var service = await _context.Services.SingleOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (service is null) return BaseResponse.Failure("25", "No record Found");
            service.Name = request.Name;
            service.Tag = request.Tag;
            service.ServiceDetailURL = request.ServiceDetailURL;
            service.ImageUrl = request.ImageUrl;
            service.BackgroundColor = request.BackgroundColor;
            service.ActiveColor = request.ActiveColor;
            _context.Services.Update(service);
            await _context.SaveChangesAsync(cancellationToken);
            return BaseResponse.Success();
        }

        public async Task<BaseResponse> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Delete Service request processing [ServiceId : {request.Id}]");
            var service = await _context.Services.SingleOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (service is null) return BaseResponse.Failure("25", "No record found");
            service.IsDeleted = true;
            _context.Services.Update(service);
            await _context.SaveChangesAsync(cancellationToken);
            return BaseResponse.Success();
        }

        public async Task<BaseResponse> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
        {
            var service = await _context.Services.FirstOrDefaultAsync(x => x.Name.ToLower() == request.Name.ToLower() && !x.IsDeleted, cancellationToken);
            if (service != null) return BaseResponse.Failure("26", "Duplicate Record - Service with this name exist");
            service = new Domain.Entities.Service
            {
                Name = request.Name,
                Tag = request.Tag,
                ServiceDetailURL = request.ServiceDetailURL,
                ImageUrl = request.ImageUrl,
                BackgroundColor = request.BackgroundColor,
                ActiveColor = request.ActiveColor
            };
            _context.Services.Add(service);
            await _context.SaveChangesAsync(cancellationToken);
            return BaseResponse.Success();
        }
    }
}
