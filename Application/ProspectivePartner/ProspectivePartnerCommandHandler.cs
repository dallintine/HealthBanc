using Application.Common.DTO;
using Application.ProspectivePartner.Command;
using MediatR;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using Domain.Entities;
using Application.Common.Interfaces;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using DocumentFormat.OpenXml.Bibliography;
using Application.Common.ConfigSettings;
using Microsoft.Extensions.Options;

namespace Application.ProspectivePartner
{
    public class ProspectivePartnerCommandHandler : IRequestHandler<CreateProspectivePartnerCommand, BaseResponse>
    {
        private readonly ILogger<ProspectivePartnerCommandHandler> _logger;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly EmailSettings _emailSettings;
        private readonly ApplicationDbContext _context;
        public ProspectivePartnerCommandHandler(ApplicationDbContext context, ILogger<ProspectivePartnerCommandHandler> logger, IEmailService emailService,
            IWebHostEnvironment environment, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _environment = environment;
            _emailSettings = emailSettings.Value;
        }
        public async Task<BaseResponse> Handle(CreateProspectivePartnerCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Prospective Partner handler Request [{JsonConvert.SerializeObject(request)} ]");

            var partner = new Domain.Entities.ProspectivePartner
            {
                Location = request.Location,
                Category = request.Category,
                Email = request.Email,
                CompanyName = request.CompanyName,
                RepresentativeName = request.RepresentativeName,
                PhoneNumber = request.PhoneNumber
            };
            _context.ProspectivePartners.Add(partner);
            await _context.SaveChangesAsync();

            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/prospectivePartnerTemplate.html";
            var htmlTemplate = File.ReadAllText(path);
            var emailTemplate = htmlTemplate.Replace("{{Name}}", request.CompanyName).Replace("{{Email}}", request.Email).Replace("{{RepresentativeName}}", request.RepresentativeName)
                .Replace("{{PhoneNumber}}", request.PhoneNumber).Replace("{{Location}}", request.Location).Replace("{{Category}}", request.Category);
            await _emailService.EmailRequest(new EmailRequest
            {
                Subject = "Become A Partner",
                Message = emailTemplate,
                Email = _emailSettings.SupportEmail
            });

            return BaseResponse.Success();
        }
    }
}
