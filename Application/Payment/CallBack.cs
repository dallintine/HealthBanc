using Application.CommonDTO;
using Application.Interfaces;
using DataAccess;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class CallBack
    {
        public class Command : IRequest<BaseResponse>
        {
            [JsonProperty("trxref")]
            public string Trxref { get; set; }
            [JsonProperty("reference")]
            public string Reference { get; set; }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IRepositoryWrapper _repositoryWrapper;
            private readonly IPaystackService _paystackService;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager, IRepositoryWrapper repositoryWrapper, IPaystackService paystackService)
            {
                _logger = logger;
                _userManager = userManager;
                _repositoryWrapper = repositoryWrapper;
                _paystackService = paystackService;
            }

            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var validatePayment = await _paystackService.VerifyPayment(request.Reference);
                if (!validatePayment.Status) return BaseResponse.Failure("06", validatePayment.Message);
                var subList = new List<Subscription>();
                foreach(var item in validatePayment.Data.Metadata.Custom_fields)
                {
                    var subscription = await _repositoryWrapper.Subscription.Find(x => x.Id == item.SubscriptionId);
                    if(subscription != null)
                    {
                        subscription.IsSuccessfully = true;
                    }
                    subList.Add(subscription);
                }
                _repositoryWrapper.Subscription.UpdateRange(subList);
                await _repositoryWrapper.Save();
                return BaseResponse<VerifyPaymentResponse>.Success(validatePayment);
            }
        }
    }
}
