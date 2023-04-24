using Application.CommonDTO;
using DataAccess;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans
{
    public class Create
    {
        public class Command : IRequest<BaseResponse>
        {
            public long ProductId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public decimal Discount { get; set; }
            public double MarkUpRate { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Name).NotEmpty().NotNull();
                RuleFor(x => x.ProductId).NotEmpty().NotNull().Must(x => x > 0).WithMessage("Product Id cannot be zero");
                RuleFor(x => x.Price).NotEmpty().NotNull().Must(x => x > 0).WithMessage("Price has to be greater than zero.");
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IRepositoryWrapper _repositoryWrapper;

            public Handler(ILogger<Handler> logger, IRepositoryWrapper repositoryWrapper)
            {
                _logger = logger;
                _repositoryWrapper = repositoryWrapper;
            }
            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var plan = await _repositoryWrapper.Plan.Find(x => x.Name.ToLower() == request.Name.ToLower() && x.ProductId == request.ProductId);
                if (plan != null) return BaseResponse.Failure("26", "Duplicate Record - Plan with this name exist");
                plan = new Plan
                {
                    ProductId = request.ProductId,
                    Name = request.Name,
                    Discount = request.Discount,
                    MarkUpRate = request.MarkUpRate,
                    Price = request.Price,  
                };
                _repositoryWrapper.Plan.Create(plan);
                await _repositoryWrapper.Save();
                return BaseResponse.Success();
            }
        }
    }
}
