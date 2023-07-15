using Application.Common.DTO;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment.Commands
{
    public class CallbackCommand : IRequest<BaseResponse>
    {
        [JsonProperty("reference")]
        public string Reference { get; set; }
    }
}
