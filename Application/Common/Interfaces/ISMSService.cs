using Application.Common.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface ISMSService
    {
        Task<BaseResponse> SendSmsAsync(string phone, string message);
    }
}
