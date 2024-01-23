using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Customer.Commands;
using Application.Image.DTO;
using Application.Service.Commands;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer
{
    public class CustomerCommandhandler : IRequestHandler<UpdateCustomerProfile, BaseResponse>,
        IRequestHandler<UpdateProfileImage, BaseResponse<ImagesURLDTO>>
    {
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerCommandhandler> _logger;
        private readonly IImageService _imageService;

        public CustomerCommandhandler(ITokenService tokenService, ApplicationDbContext context , ILogger<CustomerCommandhandler> logger, IImageService imageService)
        {
            _tokenService = tokenService;
            _context = context;
            _logger = logger;
            _imageService = imageService;
        }

        public async Task<BaseResponse> Handle(UpdateCustomerProfile request, CancellationToken cancellationToken)
        {
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
            var user = await _context.Users.Include(x => x.HealthDetail).SingleOrDefaultAsync(x => x.Id == long.Parse(userId), cancellationToken: cancellationToken);

            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName  = request.LastName ?? user.LastName;
            user.PhoneNumber = request.Phonenumber ?? user.PhoneNumber;
            user.Address = request.Address ?? user.Address;
            user.State = request.State ?? user.State;

            if(request.UpdateHealthDetail != null)
            {
                user.HealthDetail ??= new HealthDetail();
                user.HealthDetail.Height = request.UpdateHealthDetail.Height ?? user.HealthDetail.Height;
                user.HealthDetail.Weight = request.UpdateHealthDetail.Weight ?? user.HealthDetail.Weight;
                user.HealthDetail.Allergies = request.UpdateHealthDetail.Allergies ?? user.HealthDetail.Allergies;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return BaseResponse.Success();
        }

        public async Task<BaseResponse<ImagesURLDTO>> Handle(UpdateProfileImage request, CancellationToken cancellationToken)
        {
            var imageDTO = new Image.DTO.UploadImageDTO
            {
                Base64Image = request.Base64Image,
                FileName = request.FileName
            };
            var validateImageDTO = _imageService.VerifyImage(imageDTO);
            if (validateImageDTO.Code != "00") return BaseResponse<ImagesURLDTO>.Failure(null,validateImageDTO.Code,validateImageDTO.Description);

            var imageURLDTO = await _imageService.UploadPics(imageDTO);

            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
            var user = await _context.Users.Include(x => x.HealthDetail).SingleOrDefaultAsync(x => x.Id == long.Parse(userId), cancellationToken: cancellationToken);

            user.ProfileImageURL = imageURLDTO.Data.URL;
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return BaseResponse<ImagesURLDTO>.Success(imageURLDTO.Data);
        }
    }
}
