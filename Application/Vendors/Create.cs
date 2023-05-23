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

namespace Application.Vendors
{
    public class Create
    {
        public class Command : IRequest<BaseResponse>
        {
            public string Name { get; set; }
            public string SettlementAccount { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Name).NotEmpty().NotNull();
                RuleFor(x => x.SettlementAccount).NotEmpty().NotNull().Length(10,10).Must(x => long.TryParse(x, out var val)).WithMessage("Invalid Settlement Account.");
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
                var product = await _repositoryWrapper.Product.Find(x => x.Name.ToLower() == request.Name.ToLower() && !x.IsDeleted);
                if (product != null) return BaseResponse.Failure("26", "Duplicate Record - Product with this name exist");
                product = new Vendor
                {
                    Name = request.Name,
                    SettlementAccount = request.SettlementAccount,
                };
                _repositoryWrapper.Product.Create(product);
                await _repositoryWrapper.Save();
                return BaseResponse.Success();
            }
        }
    }
}
