using Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISMSService
    {
        Task<ResponseMessage> SendSmsAsync(string phone, string message);
    }
}
