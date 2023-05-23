using Application.CommonDTO;
using DataAccess;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors
{
    public class Edit
    {
        public class Command : IRequest<BaseResponse>
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string SettlementAccount { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id).NotEmpty().NotNull();
                RuleFor(x => x.Name).NotEmpty().NotNull();
                RuleFor(x => x.SettlementAccount).NotEmpty().NotNull().Length(10, 10).Must(x => long.TryParse(x, out var val)).WithMessage("Invalid Settlement Account.");
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
                _logger.LogInformation($"Edit Product request processing [Payload : {JsonConvert.SerializeObject(request)}]");
                var product = await _repositoryWrapper.Product.Find(x => x.Id == request.Id);
                if (product is null) return BaseResponse.Failure("25", "No record found");
                product.Name = request.Name;
                product.SettlementAccount = request.SettlementAccount;
                _repositoryWrapper.Product.Update(product);
                await _repositoryWrapper.Save();
                return BaseResponse.Success();
            }
        }
    }
}
