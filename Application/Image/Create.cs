using Application.CommonDTO;
using Application.Interfaces;
using DataAccess;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Image
{
    public class Create
    {
        public class Command : IRequest<BaseResponse>
        {
            public string Base64Image { get; set; }
            public string FileName { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Base64Image).NotEmpty().NotNull();
                RuleFor(x => x.FileName).NotEmpty().NotNull().Must(x => x.Length > 4).WithMessage("FileName cannot be empty or less than 3");
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IRepositoryWrapper _repositoryWrapper;
            private readonly IImageService _imageService;

            public Handler(ILogger<Handler> logger, IRepositoryWrapper repositoryWrapper, IImageService imageService)
            {
                _logger = logger;
                _repositoryWrapper = repositoryWrapper;
                _imageService = imageService;
            }
            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var response = await _imageService.UploadPics(new UploadImageDTO
                {
                    FileName = request.FileName,
                    Base64Image = request.Base64Image,
                });
                if (response.Code == "00") return BaseResponse<ImagesURLDTO>.Success(response.Data);
                return BaseResponse.Success();
            }
        }
    }
}
