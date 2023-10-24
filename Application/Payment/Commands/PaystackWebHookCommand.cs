using Application.Common.DTO;
using Application.Payment.DTO;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Payment.Commands
{
    public class PaystackWebHookCommand : IRequest<BaseResponse>
    {
        public string @event { get; set; }
        [JsonProperty("data")]
        public Data Data { get; set; }
    }
}
