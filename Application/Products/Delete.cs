using Application.CommonDTO;
using DataAccess;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Products
{
    public class Delete
    {
        public class Command : IRequest<BaseResponse>
        {
            public long Id { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id).NotEmpty().NotNull();
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
                _logger.LogInformation($"Delete Product request processing [ProductId : {request.Id}]");
                var product = await _repositoryWrapper.Product.GetProductPlans(request.Id);
                if (product is null) return BaseResponse.Failure("25", "No record found");
                product.IsDeleted = true;
                product.Plans.ForEach(x => x.IsDeleted = true);
                _repositoryWrapper.Product.Update(product);
                await _repositoryWrapper.Save();
                return BaseResponse.Success();
            }
        }
    }
}
